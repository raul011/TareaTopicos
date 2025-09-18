using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Dtos.request;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Services.Processors
{
    public sealed class GrupoMateriaProcessor : IProcessor
    {
        private readonly ServicioAContext _db;
        private readonly IIdempotencyGuard _guard;
        private readonly ILogger<GrupoMateriaProcessor> _logger;

        public GrupoMateriaProcessor(ServicioAContext db, IIdempotencyGuard guard, ILogger<GrupoMateriaProcessor> logger)
        {
            _db = db;
            _guard = guard;
            _logger = logger;
        }

        public async Task ProcessAsync(Transaccion tx, CancellationToken ct)
        {
            _logger.LogInformation("🔄 Procesando Tx {TxId} para entidad GrupoMateria, tipo {Tipo}", tx.Id, tx.TipoOperacion);

            if (await _guard.IsProcessedAsync(tx.Id, ct))
            {
                _logger.LogInformation("🟡 Tx {TxId} ya fue procesada (idempotente)", tx.Id);
                tx.Estado = "COMPLETADO";
                return;
            }

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            try
            {
                switch (tx.TipoOperacion)
                {
                    case "POST":
                        if (!await CrearGrupoMateria(tx, opts, ct)) return;
                        break;
                    case "PUT":
                        if (!await ActualizarGrupoMateria(tx, opts, ct)) return;
                        break;
                    case "DELETE":
                        if (!await EliminarGrupoMateria(tx, opts, ct)) return;
                        break;
                    default:
                        _logger.LogWarning("Operación no soportada para GrupoMateria: {TipoOperacion}", tx.TipoOperacion);
                        Skip(tx, $"Operación no soportada: {tx.TipoOperacion}");
                        return;
                }

                // Si llegamos aquí, la operación fue exitosa y no se marcó como SKIP.
                _logger.LogInformation("💾 Guardando cambios para Tx {TxId}", tx.Id);
                await _db.SaveChangesAsync(ct);

                // Solo después de guardar, marcamos como completado.
                await _guard.MarkProcessedAsync(tx.Id, ct);
                tx.Estado = "COMPLETADO";
            }
            catch (DbUpdateException ex) when ((ex.InnerException?.Message ?? ex.Message).Contains("foreign key", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError(ex, "💥 Error de FK en Tx {TxId}: {Msg}", tx.Id, ex.Message);
                Skip(tx, $"No se puede procesar la operación en GrupoMateria: una de las entidades relacionadas (Materia, Docente, etc.) no existe.");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "💥 Error de deserialización en Tx {TxId}", tx.Id);
                Skip(tx, "El payload de la transacción no es un JSON válido para GrupoMateria.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Excepción inesperada en Tx {TxId}: {Msg}", tx.Id, ex.Message);
                throw new InvalidOperationException($"Error inesperado en GrupoMateriaProcessor: {ex.Message}", ex);
            }
        }

        private async Task<bool> CrearGrupoMateria(Transaccion tx, JsonSerializerOptions opts, CancellationToken ct)
        {
            var dto = JsonSerializer.Deserialize<GrupoMateriaNaturalKeyRequestDto>(tx.Payload, opts);
            if (dto is null) throw new JsonException("Payload de GrupoMateria inválido para POST.");

            // --- Traducción de Claves Naturales a IDs ---
            var materia = await _db.Materias.FirstOrDefaultAsync(m => m.Codigo == dto.MateriaCodigo, ct);
            var docente = await _db.Docentes.FirstOrDefaultAsync(d => d.Registro == dto.DocenteRegistro, ct);
            var periodo = await _db.PeriodosAcademicos.FirstOrDefaultAsync(p => p.Gestion == dto.PeriodoGestion, ct);
            var aula = await _db.Aulas.FirstOrDefaultAsync(a => a.Codigo == dto.AulaCodigo, ct);

            if (materia is null || docente is null || periodo is null || aula is null)
            {
                Skip(tx, $"Una o más entidades referenciadas no existen (Materia: {dto.MateriaCodigo}, Docente: {dto.DocenteRegistro}, etc.).");
                return false;
            }

            // Validación de unicidad (clave natural: MateriaId, PeriodoId, Grupo)
            var yaExiste = await _db.GruposMaterias.AnyAsync(g =>
                g.MateriaId == materia.Id &&
                g.PeriodoId == periodo.Id &&
                g.Grupo == dto.Grupo, ct);

            if (yaExiste)
            {
                Skip(tx, $"Ya existe un grupo '{dto.Grupo}' para la materia '{dto.MateriaCodigo}' en el período '{dto.PeriodoGestion}'.");
                return false;
            }

            // Validar que el HorarioId exista en la base de datos.
            var horarioExiste = await _db.Horarios.AnyAsync(h => h.Id == dto.HorarioId, ct);
            if (!horarioExiste)
            {
                Skip(tx, $"El Horario con Id '{dto.HorarioId}' no existe.");
                return false;
            }

            var entity = new GrupoMateria
            {
                Grupo = dto.Grupo,
                Cupo = dto.Cupo,
                Estado = string.IsNullOrWhiteSpace(dto.Estado) ? "ACTIVO" : dto.Estado,
                MateriaId = materia.Id,
                DocenteId = docente.Id,
                PeriodoId = periodo.Id,
                HorarioId = dto.HorarioId,
                AulaId = aula.Id
            };

            _db.GruposMaterias.Add(entity);
            return true;
        }

        private async Task<bool> ActualizarGrupoMateria(Transaccion tx, JsonSerializerOptions opts, CancellationToken ct)
        {
            var payload = JsonDocument.Parse(tx.Payload).RootElement;
            var id = payload.GetProperty("Id").GetInt32();
            var dto = JsonSerializer.Deserialize<GrupoMateriaNaturalKeyRequestDto>(payload.GetProperty("Dto").GetRawText(), opts);

            if (dto is null || id <= 0) throw new JsonException("Payload de GrupoMateria inválido para PUT, requiere Id y Dto.");

            var entity = await _db.GruposMaterias.FindAsync(new object[] { id }, ct);
            if (entity is null)
            {
                Skip(tx, $"No se encontró GrupoMateria con ID {id}.");
                return false;
            }

            // --- Traducción de Claves Naturales a IDs (solo las que pueden cambiar) ---
            var docente = await _db.Docentes.FirstOrDefaultAsync(d => d.Registro == dto.DocenteRegistro, ct);
            var aula = await _db.Aulas.FirstOrDefaultAsync(a => a.Codigo == dto.AulaCodigo, ct);

            if (docente is null || aula is null)
            {
                Skip(tx, $"Docente o Aula no encontrados para la actualización.");
                return false;
            }

            entity.Grupo = dto.Grupo;
            entity.Cupo = dto.Cupo;
            entity.Estado = string.IsNullOrWhiteSpace(dto.Estado) ? entity.Estado : dto.Estado;
            entity.DocenteId = docente.Id;
            entity.HorarioId = dto.HorarioId;
            entity.AulaId = aula.Id;
            // MateriaId y PeriodoId no deberían cambiar en una actualización.
            return true;
        }

        private async Task<bool> EliminarGrupoMateria(Transaccion tx, JsonSerializerOptions opts, CancellationToken ct)
        {
            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(tx.Payload, opts);
            if (payload is null || !payload.TryGetValue("Id", out var idElem) || !idElem.TryGetInt32(out var id))
                throw new JsonException("Payload de GrupoMateria inválido para DELETE. Falta 'Id'.");

            var entity = await _db.GruposMaterias.FindAsync(new object[] { id }, ct);
            if (entity is null)
            {
                Skip(tx, $"No se encontró GrupoMateria con ID {id} para eliminar. Se ignora la operación.");
                return false;
            }

            _db.GruposMaterias.Remove(entity);
            return true;
        }

        private void Skip(Transaccion tx, string motivo)
        {
            tx.Estado = "SKIP";
            _logger.LogWarning("⚠️ Tx {TxId} marcada como SKIP: {Motivo}", tx.Id, motivo);
        }
    }
}