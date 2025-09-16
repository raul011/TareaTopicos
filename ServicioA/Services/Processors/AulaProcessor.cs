using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Services;

namespace TAREATOPICOS.ServicioA.Services.Processors
{
    public sealed class AulaProcessor : IProcessor, IQueueProcessor
    {
        private readonly ServicioAContext _db;
        private readonly IIdempotencyGuard _guard;
        private readonly ILogger<AulaProcessor> _logger;

        public AulaProcessor(ServicioAContext db, IIdempotencyGuard guard, ILogger<AulaProcessor> logger)
        {
            _db = db;
            _guard = guard;
            _logger = logger;
        }

        public async Task ProcessAsync(Transaccion tx, CancellationToken ct)
        {
            _logger.LogInformation("🔄 Procesando Tx {TxId} tipo {Tipo}", tx.Id, tx.TipoOperacion);

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
                        {
                            var aula = tx.Payload is null ? null : JsonSerializer.Deserialize<Aula>(tx.Payload, opts);
                            if (aula is null)
                            {
                                Skip(tx, "Payload vacío para POST/Aula");
                                return;
                            }

                            aula.Id = 0;
                            _db.Aulas.Add(aula);
                            await _db.SaveChangesAsync(ct);

                            await _guard.MarkProcessedAsync(tx.Id, ct);
                            tx.Estado = "COMPLETADO";
                            return;
                        }

                    case "PUT":
                        {
                            var aula = tx.Payload is null ? null : JsonSerializer.Deserialize<Aula>(tx.Payload, opts);
                            if (aula is null || aula.Id <= 0)
                            {
                                Skip(tx, "Payload inválido para PUT/Aula");
                                return;
                            }

                            var existente = await _db.Aulas.FirstOrDefaultAsync(x => x.Id == aula.Id, ct);
                            if (existente is null)
                            {
                                Skip(tx, $"Aula {aula.Id} no existe");
                                return;
                            }

                            existente.Codigo = aula.Codigo;
                            existente.Capacidad = aula.Capacidad;
                            existente.Ubicacion = aula.Ubicacion;

                            await _db.SaveChangesAsync(ct);
                            await _guard.MarkProcessedAsync(tx.Id, ct);
                            tx.Estado = "COMPLETADO";
                            return;
                        }

                    case "DELETE":
                        {
                            if (!TryGetId(tx.Payload ?? string.Empty, out var id) || id <= 0)
                            {
                                Skip(tx, "DELETE/Aula requiere Id (>0)");
                                return;
                            }

                            var entity = await _db.Aulas.FirstOrDefaultAsync(x => x.Id == id, ct);
                            if (entity is null)
                            {
                                Skip(tx, $"Aula {id} no existe");
                                return;
                            }

                            try
                            {
                                _db.Aulas.Remove(entity);
                                await _db.SaveChangesAsync(ct);

                                await _guard.MarkProcessedAsync(tx.Id, ct);
                                tx.Estado = "COMPLETADO";
                                return;
                            }
                            catch (DbUpdateException ex) when ((ex.InnerException?.Message ?? ex.Message)
                                        .Contains("foreign key", StringComparison.OrdinalIgnoreCase))
                            {
                                _logger.LogError(ex, "💥 Error de FK en Tx {TxId}: {Msg}", tx.Id, ex.Message);
                                Skip(tx, $"No se puede eliminar Aula {id}: tiene dependencias (FK).");
                                return;
                            }
                        }

                    default:
                        {
                            Skip(tx, $"Tipo {tx.TipoOperacion} no soportado para Aula");
                            return;
                        }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (DbUpdateException ex) when (IsTransient(ex)) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Excepción inesperada en Tx {TxId}: {Msg}", tx.Id, ex.Message);
                throw new InvalidOperationException($"Error inesperado en AulaProcessor: {ex.Message}", ex);
            }
        }

        private static bool TryGetId(string payload, out int id)
        {
            id = 0;

            if (int.TryParse(payload.Trim('"'), out id)) return true;

            try
            {
                using var doc = JsonDocument.Parse(payload);
                if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                    doc.RootElement.TryGetProperty("Id", out var idProp))
                {
                    if (idProp.ValueKind == JsonValueKind.Number && idProp.TryGetInt32(out id)) return true;
                    if (idProp.ValueKind == JsonValueKind.String && int.TryParse(idProp.GetString(), out id)) return true;
                }
            }
            catch { }

            return false;
        }

        private void Skip(Transaccion tx, string motivo)
        {
            tx.Estado = "SKIP";
            _logger.LogWarning("⚠️ Tx {TxId} marcada como SKIP: {Motivo}", tx.Id, motivo);
        }

        private static bool IsTransient(DbUpdateException ex)
        {
            var msg = (ex.InnerException?.Message ?? ex.Message).ToLowerInvariant();
            return msg.Contains("timeout") ||
                   msg.Contains("deadlock") ||
                   msg.Contains("could not open connection") ||
                   msg.Contains("the connection is broken") ||
                   msg.Contains("temporarily") ||
                   msg.Contains("try again");
        }
    }
}