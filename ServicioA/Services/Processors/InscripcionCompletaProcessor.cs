using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Dtos.request;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Services.Processors;

/// <summary>
/// Procesa una solicitud de inscripción completa de forma asíncrona.
/// La lógica es una réplica del endpoint síncrono para mantener la consistencia.
/// </summary>
public sealed class InscripcionCompletaProcessor : IProcessor
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IIdempotencyGuard _guard;
    private readonly ILogger<InscripcionCompletaProcessor> _logger;

    public InscripcionCompletaProcessor(IServiceScopeFactory scopeFactory, IIdempotencyGuard guard, ILogger<InscripcionCompletaProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _guard = guard;
        _logger = logger;
    }

    public async Task ProcessAsync(Transaccion tx, CancellationToken ct)
    {
        if (await _guard.IsProcessedAsync(tx.Id, ct))
        {
            _logger.LogInformation("Tx {TxId} de InscripcionCompleta ya fue procesada (idempotente).", tx.Id);
            return;
        }

        // Usamos un scope para asegurar un DbContext limpio por cada transacción.
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ServicioAContext>();

        await using var dbTransaction = await context.Database.BeginTransactionAsync(ct);

        try
        {
            var dto = JsonSerializer.Deserialize<InscripcionCompletaPorCodigosRequest>(tx.Payload!);
            if (dto == null)
            {
                await MarkSkipAsync(tx, "Payload inválido o vacío.", ct);
                return;
            }

            _logger.LogInformation("🔍 Procesando inscripción completa para estudiante {Estudiante}, período {Periodo}",
                dto.EstudianteRegistro, dto.PeriodoGestion);

            _logger.LogInformation("📋 Grupos a procesar: {Grupos}", string.Join(", ", dto.MateriaGrupoCodigos));


            // La lógica aquí es una réplica del endpoint síncrono.
            // 1. TRADUCCIÓN: Resolver Estudiante y Periodo
            var estudiante = await context.Estudiantes.FirstOrDefaultAsync(e => e.Registro == dto.EstudianteRegistro, ct);
            if (estudiante == null)
            {
                await MarkSkipAsync(tx, $"Estudiante con registro '{dto.EstudianteRegistro}' no encontrado.", ct);
                return;
            }

            var periodo = await context.PeriodosAcademicos.FirstOrDefaultAsync(p => p.Gestion == dto.PeriodoGestion, ct);
            if (periodo == null)
            {
                await MarkSkipAsync(tx, $"Período con gestión '{dto.PeriodoGestion}' no encontrado.", ct);
                return;
            }

            _logger.LogInformation("✅ Validaciones básicas pasadas (Estudiante y Periodo encontrados)");

            // 2. BUSCAR O CREAR INSCRIPCIÓN
            var inscripcion = await context.Inscripciones
                .Include(i => i.Detalles)
                .FirstOrDefaultAsync(i => i.EstudianteId == estudiante.Id && i.PeriodoId == periodo.Id, ct);

            bool esNuevaInscripcion = inscripcion == null;


            var gruposMateria = new List<GrupoMateria>();
            var errores = new List<string>();

            // 3. TRADUCCIÓN Y VALIDACIÓN INICIAL: Resolver Grupos de Materia
            foreach (var codigoCompleto in dto.MateriaGrupoCodigos.Distinct())
            {
                var parts = codigoCompleto.Split('-');
                if (parts.Length < 2) { continue; }
                var grupoNombre = parts.Last();
                var materiaCodigo = string.Join("-", parts.Take(parts.Length - 1));

                var grupo = await context.GruposMaterias
                    .Include(g => g.Materia).Include(g => g.Horario)
                    .FirstOrDefaultAsync(g => g.Materia.Codigo == materiaCodigo && g.Grupo == grupoNombre && g.PeriodoId == periodo.Id, ct);

                if (grupo != null) gruposMateria.Add(grupo);
                else errores.Add($"Grupo '{codigoCompleto}' no encontrado.");
            }

            if (errores.Any())
            {
                await MarkSkipAsync(tx, string.Join("; ", errores), ct);
                return;
            }

            _logger.LogInformation("✅ Grupos resueltos: {Count} de {Total}", gruposMateria.Count, dto.MateriaGrupoCodigos.Count);

            // 4. VALIDACIONES DE NEGOCIO DETALLADAS
            var detallesValidos = new List<DetalleInscripcion>();
            var horariosSeleccionados = new List<Horario>();
            var gruposValidos = new List<GrupoMateria>();

            foreach (var grupo in gruposMateria)
            {
                var erroresGrupo = new List<string>();
                bool grupoValido = true;

                // Validar si ya está inscrito en esta materia/grupo
                if (!esNuevaInscripcion && inscripcion!.Detalles.Any(d => d.GrupoMateriaId == grupo.Id))
                {
                    continue; // Ya está inscrito, simplemente omitir y continuar.
                }

                var inscritos = await context.DetallesInscripciones.CountAsync(d => d.GrupoMateriaId == grupo.Id, ct);
                if (inscritos >= grupo.Cupo)
                {
                    erroresGrupo.Add($"Sin cupo en {grupo.Materia.Codigo}-{grupo.Grupo}. (Cupo: {grupo.Cupo}, Inscritos: {inscritos})");
                    grupoValido = false;
                }

                var prerequisitos = await context.Prerequisitos.Where(p => p.MateriaId == grupo.MateriaId).Select(p => p.MateriaPrerequisitoId).ToListAsync(ct);
                if (prerequisitos.Any())
                {
                    var materiasAprobadas = await context.HistorialesAcademicos
                        .Where(h => h.DetalleInscripcion.Inscripcion.EstudianteId == estudiante.Id && h.Aprobado)
                        .Select(h => h.DetalleInscripcion.GrupoMateria.MateriaId).ToListAsync(ct);

                    var faltantes = prerequisitos.Except(materiasAprobadas).ToList();
                    if (faltantes.Any())
                    {
                        erroresGrupo.Add($"Faltan {faltantes.Count} prerrequisitos para {grupo.Materia.Codigo}.");
                        grupoValido = false;
                    }
                }

                if (grupo.Horario != null)
                {
                    var tieneConflicto = horariosSeleccionados.Any(h =>
                        h.Dia == grupo.Horario.Dia &&
                        h.HoraInicio < grupo.Horario.HoraFin &&
                        grupo.Horario.HoraInicio < h.HoraFin);

                    if (tieneConflicto)
                    {
                        erroresGrupo.Add($"Choque de horario para {grupo.Materia.Codigo}-{grupo.Grupo}.");
                        grupoValido = false;
                    }
                }

                if (grupoValido)
                {
                    gruposValidos.Add(grupo);
                    if (grupo.Horario != null)
                    {
                        horariosSeleccionados.Add(grupo.Horario);
                    }
                }
                else
                {
                    errores.AddRange(erroresGrupo);
                }
            }

            if (!gruposValidos.Any())
            {
                await MarkSkipAsync(tx, "Ningún grupo válido para inscribir: " + string.Join("; ", errores), ct);
                return;
            }

            // 5. CREAR DETALLES VÁLIDOS
            foreach (var grupo in gruposValidos)
            {
                detallesValidos.Add(new DetalleInscripcion { Codigo = $"{grupo.Materia.Codigo}-{grupo.Grupo}", Estado = "INSCRITO", GrupoMateriaId = grupo.Id });
            }

            // 6. EJECUCIÓN: Crear la inscripción y sus detalles
            if (esNuevaInscripcion)
            {
                inscripcion = new Inscripcion { Fecha = DateTime.UtcNow, Estado = "confirmada", EstudianteId = estudiante.Id, PeriodoId = periodo.Id, Detalles = detallesValidos };
                context.Inscripciones.Add(inscripcion);
            }
            else
            {
                // Agregar los nuevos detalles a la inscripción existente
                foreach (var detalle in detallesValidos)
                {
                    inscripcion.Detalles.Add(detalle);
                }
            }

            await context.SaveChangesAsync(ct);
            await dbTransaction.CommitAsync(ct);

            _logger.LogInformation("✅ Inscripción completa creada exitosamente para Tx {TxId}", tx.Id);
            await _guard.MarkProcessedAsync(tx.Id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando Tx {TxId} de InscripcionCompleta.", tx.Id);
            await dbTransaction.RollbackAsync(ct);
            await MarkSkipAsync(tx, $"ERROR: {ex.Message}", ct);
        }
    }

    private async Task MarkSkipAsync(Transaccion tx, string motivo, CancellationToken ct)
    {
        _logger.LogWarning("Tx {TxId} de InscripcionCompleta marcada como SKIP: {Motivo}", tx.Id, motivo);
        tx.Estado = "SKIP";
        await _guard.MarkProcessedAsync(tx.Id, ct);
    }
}