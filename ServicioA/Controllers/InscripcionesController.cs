using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos;
using TAREATOPICOS.ServicioA.Dtos.request;
using TAREATOPICOS.ServicioA.Dtos.response;

using TAREATOPICOS.ServicioA.Services;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("api/inscripciones-sync")] // <-- CAMBIO: Ruta explícita para evitar ambigüedad
// [Authorize]
public class InscripcionesController : ControllerBase
{
    private readonly ServicioAContext _context;
    private readonly QueueManager _qm;
    private readonly IConfiguration _cfg;
    private readonly WorkerHost _workerHost;
    public InscripcionesController(ServicioAContext context, QueueManager qm, IConfiguration cfg,WorkerHost workerHost)
    {
        _context = context;
        _qm = qm;
        _cfg = cfg;
        _workerHost = workerHost;
    }


    // POST: api/inscripciones/completa - NUEVO: Inscripción completa con múltiples materias
    [HttpPost("completa")]
    public async Task<ActionResult<InscripcionCompletaResponseDto>> CreateCompleta([FromBody] InscripcionCompletaRequestDto dto, CancellationToken ct)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(ct);

        try
        {
            var response = new InscripcionCompletaResponseDto();
            var errores = new List<string>();

            // 1. Validar estudiante y período
            var estudiante = await _context.Estudiantes
                .Include(e => e.Carrera)
                .FirstOrDefaultAsync(e => e.Id == dto.EstudianteId, ct);

            var periodo = await _context.PeriodosAcademicos
                .FirstOrDefaultAsync(p => p.Id == dto.PeriodoId, ct);

            if (estudiante == null)
            {
                errores.Add("Estudiante no encontrado");
                response.Exito = false;
            }

            if (periodo == null)
            {
                errores.Add("Período académico no encontrado");
                response.Exito = false;
            }

            if (!response.Exito)
            {
                response.Errores = errores;
                return BadRequest(response);
            }

            // 2. Verificar inscripción duplicada
            var inscripcionExistente = await _context.Inscripciones
                .AnyAsync(i => i.EstudianteId == dto.EstudianteId && i.PeriodoId == dto.PeriodoId, ct);

            if (inscripcionExistente)
            {
                errores.Add("El estudiante ya tiene una inscripción en este período");
                response.Exito = false;
                response.Errores = errores;
                return Conflict(response);
            }

            // 3. Obtener grupos de materia con información completa
            var gruposMateria = await _context.GruposMaterias
                .Include(g => g.Materia)
                .Include(g => g.Docente)
                .Include(g => g.Horario)
                .Include(g => g.Aula)
                .Where(g => dto.GrupoMateriaIds.Contains(g.Id))
                .ToListAsync(ct);

            if (gruposMateria.Count != dto.GrupoMateriaIds.Count)
            {
                var encontrados = gruposMateria.Select(g => g.Id).ToList();
                var noEncontrados = dto.GrupoMateriaIds.Except(encontrados);
                errores.Add($"Grupos de materia no encontrados: {string.Join(", ", noEncontrados)}");
                response.Exito = false;
            }

            if (!response.Exito)
            {
                response.Errores = errores;
                return BadRequest(response);
            }

            // 4. Validaciones de negocio
            var detallesValidos = new List<DetalleInscripcion>();
            var detallesResponse = new List<DetalleInscripcionCompletoDto>();

            foreach (var grupo in gruposMateria)
            {
                var detalleErrores = new List<string>();

                // Validar cupo disponible
                var inscritosEnGrupo = await _context.DetallesInscripciones
                    .CountAsync(d => d.GrupoMateriaId == grupo.Id, ct);

                if (inscritosEnGrupo >= grupo.Cupo)
                {
                    detalleErrores.Add($"Grupo {grupo.Grupo} sin cupo disponible (Cupo: {grupo.Cupo}, Inscritos: {inscritosEnGrupo})");
                }

                // Validar prerequisitos
                var prerequisitos = await _context.Prerequisitos
                    .Include(p => p.MateriaPrerequisito)
                    .Where(p => p.MateriaId == grupo.MateriaId)
                    .ToListAsync(ct);

                foreach (var prerequisito in prerequisitos)
                {
                    var tienePrerequisito = await _context.DetallesInscripciones
                        .Include(d => d.GrupoMateria)
                            .ThenInclude(g => g.Materia)
                        .AnyAsync(d => d.Inscripcion.EstudianteId == dto.EstudianteId &&
                                      d.GrupoMateria.MateriaId == prerequisito.MateriaPrerequisitoId &&
                                      d.Estado == "APROBADO", ct);

                    if (!tienePrerequisito)
                    {
                        detalleErrores.Add($"Falta prerequisito: {prerequisito.MateriaPrerequisito.Nombre}");
                    }
                }

                // Validar conflictos de horario (simplificado)
                var horarioConflicto = await _context.DetallesInscripciones
                    .Include(d => d.GrupoMateria)
                        .ThenInclude(g => g.Horario)
                    .Where(d => d.Inscripcion.EstudianteId == dto.EstudianteId &&
                               d.Inscripcion.PeriodoId == dto.PeriodoId &&
                               d.GrupoMateria.HorarioId == grupo.HorarioId)
                    .AnyAsync(ct);

                if (horarioConflicto)
                {
                    detalleErrores.Add($"Conflicto de horario con otra materia");
                }

                if (detalleErrores.Any())
                {
                    errores.AddRange(detalleErrores.Select(e => $"{grupo.Materia.Nombre} ({grupo.Grupo}): {e}"));
                    response.Exito = false;
                }
                else
                {
                    // Crear detalle de inscripción
                    var detalle = new DetalleInscripcion
                    {
                        Codigo = $"{grupo.Materia.Codigo}-{grupo.Grupo}",
                        Estado = "INSCRITO",
                        GrupoMateriaId = grupo.Id,
                        InscripcionId = 0 // Se asignará después
                    };

                    detallesValidos.Add(detalle);

                    // Crear respuesta del detalle
                    detallesResponse.Add(new DetalleInscripcionCompletoDto
                    {
                        Id = detalle.Id,
                        Codigo = detalle.Codigo,
                        Estado = detalle.Estado,
                        MateriaNombre = grupo.Materia.Nombre,
                        Grupo = grupo.Grupo,
                        DocenteNombre = grupo.Docente.Nombre,
                        Horario = grupo.Horario != null ? $"{grupo.Horario.Dia} {grupo.Horario.HoraInicio} - {grupo.Horario.HoraFin}" : null,
                        Aula = grupo.Aula?.Codigo,
                        CupoDisponible = grupo.Cupo - inscritosEnGrupo - 1,
                        Observaciones = dto.Observaciones
                    });
                }
            }

            if (!response.Exito)
            {
                response.Errores = errores;
                return BadRequest(response);
            }

            // 5. Crear inscripción y detalles (todo en transacción)
            var inscripcion = new Inscripcion
            {
                Fecha = DateTime.UtcNow,
                Estado = "PENDIENTE",
                EstudianteId = dto.EstudianteId,
                PeriodoId = dto.PeriodoId,
                Detalles = detallesValidos
            };

            _context.Inscripciones.Add(inscripcion);
            await _context.SaveChangesAsync(ct);

            // Actualizar IDs de detalles
            foreach (var detalle in detallesValidos)
            {
                detalle.InscripcionId = inscripcion.Id;
            }

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            // 6. Preparar respuesta exitosa
            response.Id = inscripcion.Id;
            response.Fecha = inscripcion.Fecha;
            response.Estado = inscripcion.Estado;
            response.Observaciones = dto.Observaciones;
            response.Estudiante = new EstudianteResponseDto
            {
                Registro = estudiante.Registro,
                Ci = estudiante.Ci,
                Nombre = estudiante.Nombre,
                Email = estudiante.Email,
                Telefono = estudiante.Telefono,
                Direccion = estudiante.Direccion,
                Estado = estudiante.Estado,
                Carrera = new CarreraDto
                {
                    Id = estudiante.Carrera.Id,
                    Nombre = estudiante.Carrera.Nombre
                }
            };
            response.Periodo = new PeriodoAcademicoResponseDto
            {
                Gestion = periodo.Gestion,
                FechaInicio = periodo.FechaInicio,
                FechaFin = periodo.FechaFin
            };
            response.Detalles = detallesResponse;
            response.Errores = errores;

            return CreatedAtAction(nameof(GetById), new { id = inscripcion.Id }, response);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            var errorResponse = new InscripcionCompletaResponseDto
            {
                Exito = false,
                Errores = new List<string> { $"Error interno: {ex.Message}" }
            };
            return StatusCode(500, errorResponse);
        }
    }

    // =====================================================================================
    // === NUEVOS ENDPOINTS PARA ESTUDIO ACADÉMICO (SÍNCRONO vs ASÍNCRONO) =================
    // =====================================================================================

    /// <summary>
    /// [SÍNCRONO] Crea una inscripción completa para un estudiante en un período,
    /// usando identificadores de negocio. El cliente espera a que toda la operación termine.
    /// </summary>
    [HttpPost("sync/completa")]
    public async Task<IActionResult> CrearInscripcionCompletaSync([FromBody] InscripcionCompletaPorCodigosRequest dto, CancellationToken ct)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);

        try
        {
            // 1. TRADUCCIÓN: Resolver Estudiante y Periodo
            var estudiante = await _context.Estudiantes.FirstOrDefaultAsync(e => e.Registro == dto.EstudianteRegistro, ct);
            if (estudiante == null) return BadRequest(new { error = $"Estudiante con registro '{dto.EstudianteRegistro}' no encontrado." });

            var periodo = await _context.PeriodosAcademicos.FirstOrDefaultAsync(p => p.Gestion == dto.PeriodoGestion, ct);
            if (periodo == null) return BadRequest(new { error = $"Período con gestión '{dto.PeriodoGestion}' no encontrado." });

            // 2. BUSCAR O CREAR INSCRIPCIÓN: En lugar de fallar si existe, la reutilizamos.
            var inscripcion = await _context.Inscripciones
                .Include(i => i.Detalles) // Cargar detalles existentes
                .FirstOrDefaultAsync(i => i.EstudianteId == estudiante.Id && i.PeriodoId == periodo.Id, ct);

            bool esNuevaInscripcion = inscripcion == null;
            if (esNuevaInscripcion) { /* Se creará más adelante si todo es válido */ }

            var gruposMateria = new List<GrupoMateria>();
            var errores = new List<string>();

            // 3. TRADUCCIÓN Y VALIDACIÓN INICIAL: Resolver Grupos de Materia
            foreach (var codigoCompleto in dto.MateriaGrupoCodigos.Distinct())
            {
                var parts = codigoCompleto.Split('-');
                if (parts.Length < 2)
                {
                    errores.Add($"El código '{codigoCompleto}' tiene un formato inválido. Debe ser 'CODIGOMATERIA-GRUPO'.");
                    continue;
                }
                var grupoNombre = parts.Last();
                var materiaCodigo = string.Join("-", parts.Take(parts.Length - 1));

                var grupo = await _context.GruposMaterias
                    .Include(g => g.Materia)
                    .Include(g => g.Horario)
                    .FirstOrDefaultAsync(g => g.Materia.Codigo == materiaCodigo && g.Grupo == grupoNombre && g.PeriodoId == periodo.Id, ct);

                if (grupo != null)
                {
                    gruposMateria.Add(grupo);
                }
                else
                {
                    errores.Add($"El grupo '{codigoCompleto}' no fue encontrado para el período '{dto.PeriodoGestion}'.");
                }
            }

            if (errores.Any()) return BadRequest(new { errores });

            // 4. VALIDACIONES DE NEGOCIO DETALLADAS
            var detallesValidos = new List<DetalleInscripcion>();
            var horariosSeleccionados = new List<Horario>();

            foreach (var grupo in gruposMateria)
            {
                // Validar si ya está inscrito en esta materia/grupo
                if (!esNuevaInscripcion && inscripcion.Detalles.Any(d => d.GrupoMateriaId == grupo.Id))
                {
                    continue; // Ya está inscrito, simplemente omitir y continuar con el siguiente.
                }

                // Validar cupo
                var inscritos = await _context.DetallesInscripciones.CountAsync(d => d.GrupoMateriaId == grupo.Id, ct);
                if (inscritos >= grupo.Cupo)
                {
                    errores.Add($"Sin cupo en {grupo.Materia.Codigo}-{grupo.Grupo}. (Límite: {grupo.Cupo})");
                }

                // Validar prerrequisitos (simplificado: asume que el historial está correcto)
                var prerequisitos = await _context.Prerequisitos
                    .Where(p => p.MateriaId == grupo.MateriaId)
                    .Select(p => p.MateriaPrerequisitoId)
                    .ToListAsync(ct);

                if (prerequisitos.Any())
                {
                    var materiasAprobadas = await _context.HistorialesAcademicos
                        .Where(h => h.DetalleInscripcion.Inscripcion.EstudianteId == estudiante.Id && h.Aprobado)
                        .Select(h => h.DetalleInscripcion.GrupoMateria.MateriaId)
                        .ToListAsync(ct);

                    var faltantes = prerequisitos.Except(materiasAprobadas).Count();
                    if (faltantes > 0)
                    {
                        errores.Add($"Faltan {faltantes} prerrequisitos para {grupo.Materia.Codigo}.");
                    }
                }

                // Validar choque de horarios
                if (grupo.Horario != null)
                {
                    if (horariosSeleccionados.Any(h => h.Dia == grupo.Horario.Dia && h.HoraInicio < grupo.Horario.HoraFin && grupo.Horario.HoraInicio < h.HoraFin))
                    {
                        errores.Add($"Choque de horario para {grupo.Materia.Codigo}-{grupo.Grupo}.");
                    }
                    else
                    {
                        horariosSeleccionados.Add(grupo.Horario);
                    }
                }

                if (!errores.Any())
                {
                    detallesValidos.Add(new DetalleInscripcion
                    {
                        Codigo = $"{grupo.Materia.Codigo}-{grupo.Grupo}",
                        Estado = "INSCRITO",
                        GrupoMateriaId = grupo.Id
                    });
                }
            }

            if (errores.Any()) return BadRequest(new { errores });

            // 5. EJECUCIÓN: Crear la inscripción y sus detalles
            if (esNuevaInscripcion)
            {
                inscripcion = new Inscripcion
                {
                    Fecha = DateTime.UtcNow,
                    Estado = "PENDIENTE",
                    EstudianteId = estudiante.Id,
                    PeriodoId = periodo.Id,
                    Detalles = detallesValidos
                };
                _context.Inscripciones.Add(inscripcion);
            }
            else
            {
                // Agregar los nuevos detalles a la inscripción existente
                foreach (var detalle in detallesValidos) { inscripcion.Detalles.Add(detalle); }
            }

            await _context.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);

            // 6. RESPUESTA: Devolver el objeto creado
            return CreatedAtAction(nameof(GetById), new { id = inscripcion.Id }, new { inscripcionId = inscripcion.Id, message = "Inscripción completa creada exitosamente." });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            return StatusCode(500, new { error = "Error interno del servidor.", details = ex.Message });
        }
    }

    /// <summary>
    /// [ASÍNCRONO] Encola una solicitud de inscripción completa.
    /// Responde inmediatamente con un ID de transacción para seguimiento.
    /// </summary>
    [HttpPost("async/completa")]
    public async Task<IActionResult> CrearInscripcionCompletaAsync([FromBody] InscripcionCompletaPorCodigosRequest dto, CancellationToken ct)
    {
        if (dto.MateriaGrupoCodigos == null || !dto.MateriaGrupoCodigos.Any())
        {
            return BadRequest(new { error = "Debe proporcionar al menos un código de materia/grupo." });
        }

        var tx = new Transaccion
        {
            Entidad = "InscripcionCompleta", // Nuevo tipo de entidad para el procesador
            TipoOperacion = "Crear",
            Payload = JsonSerializer.Serialize(dto),
            Estado = "EN_COLA",
            Priority = 1, // Prioridad media por defecto
            NotBefore = DateTimeOffset.UtcNow,
            CallbackUrl = _cfg["Webhook:DefaultUrl"],
            CallbackSecret = _cfg["Webhook:DefaultSecret"],
            IdempotencyKey = Guid.NewGuid().ToString() // Clave única para esta operación
        };

        await _qm.EnqueueAsync(tx, "default", ct);

        return Accepted(new { transaccionId = tx.Id, estado = tx.Estado });
    }

    // GET: api/inscripciones/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<InscripcionResponseDto>> GetById(int id, CancellationToken ct)
    {
        var i = await _context.Inscripciones
            .AsNoTracking()
            .Include(x => x.Estudiante)
                .ThenInclude(e => e.Carrera)
            .Include(x => x.Periodo)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return i is null ? NotFound() : Ok(ToResponseDTO(i));
    }

  

    // GET: api/inscripciones/{id}/detalles
    [HttpGet("{id:int}/detalles")]
    public async Task<ActionResult<IEnumerable<DetalleInscripcionDto>>> GetDetalles(int id, CancellationToken ct)
    {
        var detalles = await _context.DetallesInscripciones
            .AsNoTracking()
            .Where(d => d.InscripcionId == id)
            .ToListAsync(ct);

        return Ok(detalles.Select(ToDTO));
    }

 

    // PUT: api/inscripciones/{id}/estado
    [HttpPut("{id:int}/estado")]
    public async Task<IActionResult> UpdateEstado(int id, [FromBody] string estado, CancellationToken ct)
    {
        var inscripcion = await _context.Inscripciones.FindAsync(new object[] { id }, ct);
        if (inscripcion is null) return NotFound();

        var estadosValidos = new[] { "PENDIENTE", "FINALIZADA", "CANCELADA", "APROBADA" };
        if (!estadosValidos.Contains(estado.ToUpper()))
            return BadRequest($"Estado inválido. Estados válidos: {string.Join(", ", estadosValidos)}");

        inscripcion.Estado = estado.ToUpper();
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    // DELETE: api/inscripciones/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var inscripcion = await _context.Inscripciones
            .Include(i => i.Detalles)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (inscripcion is null) return NotFound();

        if (inscripcion.Estado == "FINALIZADA")
            return BadRequest("No se puede eliminar una inscripción finalizada");

        _context.Inscripciones.Remove(inscripcion);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    // POST: api/inscripciones/{id}/materia
    [HttpPost("{id:int}/materia")]
    public async Task<ActionResult<DetalleInscripcionDto>> AddMateria(int id, [FromBody] DetalleInscripcionCreateDto dto, CancellationToken ct)
    {
        var inscripcion = await _context.Inscripciones
            .Include(i => i.Detalles)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (inscripcion is null) return NotFound();
        if (inscripcion.Estado == "FINALIZADA") return BadRequest("No se puede agregar materias a una inscripción finalizada");

        // Verificar que el grupo de materia existe
        var grupoMateria = await _context.GruposMaterias
            .Include(g => g.Materia)
            .FirstOrDefaultAsync(g => g.Id == dto.GrupoMateriaId, ct);

        if (grupoMateria is null) return BadRequest("Grupo de materia no encontrado");

        // Verificar que no esté ya inscrito en esta materia
        var yaInscrito = inscripcion.Detalles.Any(d => d.GrupoMateriaId == dto.GrupoMateriaId);
        if (yaInscrito) return Conflict("El estudiante ya está inscrito en esta materia");

        // Validar cupo
        var inscritosEnGrupo = await _context.DetallesInscripciones
            .CountAsync(d => d.GrupoMateriaId == dto.GrupoMateriaId, ct);

        if (inscritosEnGrupo >= grupoMateria.Cupo)
            return BadRequest("No hay cupo disponible en este grupo");

        // Crear detalle de inscripción
        var detalle = new DetalleInscripcion
        {
            Codigo = $"{grupoMateria.Materia.Codigo}-{grupoMateria.Grupo}",
            Estado = "INSCRITO",
            InscripcionId = id,
            GrupoMateriaId = dto.GrupoMateriaId
        };

        _context.DetallesInscripciones.Add(detalle);
        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetDetalles), new { id = id }, ToDTO(detalle));
    }

    // DELETE: api/inscripciones/{inscripcionId}/materia/{detalleId}
    [HttpDelete("{inscripcionId:int}/materia/{detalleId:int}")]
    public async Task<IActionResult> RemoveMateria(int inscripcionId, int detalleId, CancellationToken ct)
    {
        var detalle = await _context.DetallesInscripciones
            .FirstOrDefaultAsync(d => d.Id == detalleId && d.InscripcionId == inscripcionId, ct);

        if (detalle is null) return NotFound();

        var inscripcion = await _context.Inscripciones
            .FirstOrDefaultAsync(i => i.Id == inscripcionId, ct);

        if (inscripcion?.Estado == "FINALIZADA")
            return BadRequest("No se puede remover materias de una inscripción finalizada");

        _context.DetallesInscripciones.Remove(detalle);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    // GET: api/inscripciones/validar
    [HttpPost("validar")]
    public async Task<ActionResult<InscripcionCompletaResponseDto>> ValidarInscripcion([FromBody] InscripcionCompletaRequestDto dto, CancellationToken ct)
    {
        var response = new InscripcionCompletaResponseDto();
        var errores = new List<string>();

        // 1. Validar estudiante y período
        var estudiante = await _context.Estudiantes
            .Include(e => e.Carrera)
            .FirstOrDefaultAsync(e => e.Id == dto.EstudianteId, ct);

        var periodo = await _context.PeriodosAcademicos
            .FirstOrDefaultAsync(p => p.Id == dto.PeriodoId, ct);

        if (estudiante == null) errores.Add("Estudiante no encontrado");
        if (periodo == null) errores.Add("Período académico no encontrado");

        if (errores.Any())
        {
            response.Exito = false;
            response.Errores = errores;
            return BadRequest(response);
        }

        // 2. Obtener grupos de materia
        var gruposMateria = await _context.GruposMaterias
            .Include(g => g.Materia)
            .Include(g => g.Docente)
            .Include(g => g.Horario)
            .Include(g => g.Aula)
            .Where(g => dto.GrupoMateriaIds.Contains(g.Id))
            .ToListAsync(ct);

        if (gruposMateria.Count != dto.GrupoMateriaIds.Count)
        {
            var encontrados = gruposMateria.Select(g => g.Id).ToList();
            var noEncontrados = dto.GrupoMateriaIds.Except(encontrados);
            errores.Add($"Grupos de materia no encontrados: {string.Join(", ", noEncontrados)}");
        }

        // 3. Validaciones detalladas
        var detallesResponse = new List<DetalleInscripcionCompletoDto>();

        foreach (var grupo in gruposMateria)
        {
            var detalleErrores = new List<string>();

            // Validar cupo disponible
            var inscritosEnGrupo = await _context.DetallesInscripciones
                .CountAsync(d => d.GrupoMateriaId == grupo.Id, ct);

            if (inscritosEnGrupo >= grupo.Cupo)
            {
                detalleErrores.Add($"Grupo {grupo.Grupo} sin cupo disponible (Cupo: {grupo.Cupo}, Inscritos: {inscritosEnGrupo})");
            }

            // Validar prerequisitos
            var prerequisitos = await _context.Prerequisitos
                .Include(p => p.MateriaPrerequisito)
                .Where(p => p.MateriaId == grupo.MateriaId)
                .ToListAsync(ct);

            foreach (var prerequisito in prerequisitos)
            {
                var tienePrerequisito = await _context.DetallesInscripciones
                    .Include(d => d.GrupoMateria)
                        .ThenInclude(g => g.Materia)
                    .AnyAsync(d => d.Inscripcion.EstudianteId == dto.EstudianteId &&
                                  d.GrupoMateria.MateriaId == prerequisito.MateriaPrerequisitoId &&
                                  d.Estado == "APROBADO", ct);

                if (!tienePrerequisito)
                {
                    detalleErrores.Add($"Falta prerequisito: {prerequisito.MateriaPrerequisito.Nombre}");
                }
            }

            // Crear respuesta del detalle
            detallesResponse.Add(new DetalleInscripcionCompletoDto
            {
                Id = 0,
                Codigo = $"{grupo.Materia.Codigo}-{grupo.Grupo}",
                Estado = detalleErrores.Any() ? "ERROR" : "VALIDO",
                MateriaNombre = grupo.Materia.Nombre,
                Grupo = grupo.Grupo,
                DocenteNombre = grupo.Docente.Nombre,
                Horario = grupo.Horario != null ? $"{grupo.Horario.Dia} {grupo.Horario.HoraInicio} - {grupo.Horario.HoraFin}" : null,
                Aula = grupo.Aula?.Codigo,
                CupoDisponible = grupo.Cupo - inscritosEnGrupo,
                Observaciones = string.Join("; ", detalleErrores)
            });
        }

        response.Id = 0;
        response.Fecha = DateTime.UtcNow;
        response.Estado = errores.Any() ? "CON_ERRORES" : "VALIDA";
        response.Estudiante = new EstudianteResponseDto
        {
            Registro = estudiante.Registro,
            Ci = estudiante.Ci,
            Nombre = estudiante.Nombre,
            Email = estudiante.Email,
            Telefono = estudiante.Telefono,
            Direccion = estudiante.Direccion,
            Estado = estudiante.Estado,
            Carrera = new CarreraDto
            {
                Id = estudiante.Carrera.Id,
                Nombre = estudiante.Carrera.Nombre
            }
        };
        response.Periodo = new PeriodoAcademicoResponseDto
        {
            Gestion = periodo.Gestion,
            FechaInicio = periodo.FechaInicio,
            FechaFin = periodo.FechaFin
        };
        response.Detalles = detallesResponse;
        response.Errores = errores;
        response.Exito = !errores.Any();

        return Ok(response);
    }

    // Utilidades de mapeo
    private static InscripcionRequestDto ToDTO(Inscripcion i) => new()
    {
        Id = i.Id,
        Fecha = i.Fecha,
        Estado = i.Estado,
        EstudianteId = i.EstudianteId,
        PeriodoId = i.PeriodoId
    };

    private static InscripcionResponseDto ToResponseDTO(Inscripcion i) => new()
    {
        Id = i.Id,
        Fecha = i.Fecha,
        Estado = i.Estado,
        Estudiante = new EstudianteResponseDto
        {
            Registro = i.Estudiante.Registro,
            Ci = i.Estudiante.Ci,
            Nombre = i.Estudiante.Nombre,
            Email = i.Estudiante.Email,
            Telefono = i.Estudiante.Telefono,
            Direccion = i.Estudiante.Direccion,
            Estado = i.Estudiante.Estado,
            Carrera = new CarreraDto
            {
                Id = i.Estudiante.Carrera.Id,
                Nombre = i.Estudiante.Carrera.Nombre
            }
        },
        Periodo = new PeriodoAcademicoResponseDto
        {
            Gestion = i.Periodo.Gestion,
            FechaInicio = i.Periodo.FechaInicio,
            FechaFin = i.Periodo.FechaFin
        }
    };

    private static DetalleInscripcionDto ToDTO(DetalleInscripcion d) => new()
    {
        Id = d.Id,
        Codigo = d.Codigo,
        Estado = d.Estado,
        InscripcionId = d.InscripcionId,
        GrupoMateriaId = d.GrupoMateriaId
    };
}
