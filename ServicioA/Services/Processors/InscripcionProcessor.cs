using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Services.Processors;

public sealed class InscripcionProcessor : IProcessor
{
    private readonly ServicioAContext _db;
    private readonly ILogger<InscripcionProcessor> _log;

    public InscripcionProcessor(ServicioAContext db, ILogger<InscripcionProcessor> log)
    {
        _db = db;
        _log = log;
    }

    private record Payload(string Registro, int PeriodoId, List<MateriaGrupo> Materias);
    private record MateriaGrupo(string MateriaCodigo, string Grupo);

    public async Task ProcessAsync(Transaccion tx, CancellationToken ct)
    {
        _log.LogInformation("📥 Procesando Tx {TxId} tipo {Tipo}", tx.Id, tx.TipoOperacion);

        await using var dbTx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var payload = JsonSerializer.Deserialize<Payload>(tx.Payload ?? "");
            if (payload == null)
            {
                Skip(tx, "Payload inválido para Inscripción");
                return;
            }

            var estudiante = await _db.Estudiantes
                .FirstOrDefaultAsync(e => e.Registro == payload.Registro, ct);
            if (estudiante == null)
            {
                Skip(tx, $"Estudiante {payload.Registro} no encontrado");
                return;
            }

            // Crear la inscripción principal
            var inscripcion = new Inscripcion
            {
                EstudianteId = estudiante.Id,
                PeriodoId = payload.PeriodoId,
                Fecha = DateTime.UtcNow,
                Estado = "PENDIENTE"
            };

            _db.Inscripciones.Add(inscripcion);
            await _db.SaveChangesAsync(ct);

            int confirmadas = 0;
            int total = payload.Materias.Count;

            foreach (var m in payload.Materias)
            {
                var grupo = await _db.GruposMaterias
                    .Include(g => g.Materia)
                    .FirstOrDefaultAsync(g =>
                        g.Materia.Codigo == m.MateriaCodigo &&
                        g.Grupo == m.Grupo &&
                        g.PeriodoId == payload.PeriodoId, ct);

                if (grupo == null)
                {
                    _log.LogWarning("⚠️ Grupo no encontrado para {MateriaCodigo}-{Grupo}", m.MateriaCodigo, m.Grupo);
                    continue;
                }

                // Verificar cupos disponibles (uso COUNT real para evitar desincronización)
                int cuposActuales = await _db.DetallesInscripciones
                    .CountAsync(d => d.GrupoMateriaId == grupo.Id, ct);

                if (cuposActuales >= grupo.Cupo)
                {
                    _log.LogWarning("❌ Sin cupos disponibles para {MateriaCodigo}-{Grupo}", m.MateriaCodigo, m.Grupo);
                    continue;
                }

                // Generar código único del detalle
                string codigoDetalle = $"{m.MateriaCodigo}-{m.Grupo}-{inscripcion.Id}";

                // Crear detalle
                var detalle = new DetalleInscripcion
                {
                    Codigo = codigoDetalle,
                    Estado = "INSCRITO",
                    InscripcionId = inscripcion.Id,
                    GrupoMateriaId = grupo.Id,
                    NotaFinal = null
                };
                _db.DetallesInscripciones.Add(detalle);

                // 🔽 Reducir cupo visual (opcional, pero útil para ver cambios en BD)
                if (grupo.Cupo > 0)
                {
                    grupo.Cupo -= 1;
                    _log.LogInformation("📉 Cupo reducido para {MateriaCodigo}-{Grupo}: {CupoRestante}",
                        m.MateriaCodigo, m.Grupo, grupo.Cupo);
                }

                confirmadas++;
            }

            // Actualizar estado final de la inscripción
            inscripcion.Estado = confirmadas switch
            {
                0 => "RECHAZADA",
                var c when c < total => "PARCIAL",
                _ => "CONFIRMADA"
            };

            await _db.SaveChangesAsync(ct);
            await dbTx.CommitAsync(ct);

            tx.Estado = "COMPLETADO";
            _log.LogInformation("✅ Tx {TxId} procesada correctamente: {Confirmadas}/{Total} confirmadas",
                tx.Id, confirmadas, total);
        }
        catch (Exception ex)
        {
            await dbTx.RollbackAsync(ct);
            _log.LogError(ex, "💥 Error procesando inscripción Tx={TxId}", tx.Id);
            tx.Estado = "ERROR";
        }
    }

    private void Skip(Transaccion tx, string motivo)
    {
        tx.Estado = "SKIP";
        _log.LogWarning("⚠️ Tx {TxId} marcada como SKIP: {Motivo}", tx.Id, motivo);
    }
}
