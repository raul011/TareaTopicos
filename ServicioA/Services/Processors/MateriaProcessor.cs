using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Dtos.request;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Services.Processors
{
    public sealed class MateriaProcessor : IProcessor
    {
        private readonly ServicioAContext _db;
        private readonly IIdempotencyGuard _guard;
        private readonly ILogger<MateriaProcessor> _logger;

        public MateriaProcessor(ServicioAContext db, IIdempotencyGuard guard, ILogger<MateriaProcessor> logger)
        {
            _db = db;
            _guard = guard;
            _logger = logger;
        }

        public async Task ProcessAsync(Transaccion tx, CancellationToken ct)
        {
            _logger.LogInformation("Procesando Tx {TxId} para entidad Materia, tipo {Tipo}", tx.Id, tx.TipoOperacion);

            if (await _guard.IsProcessedAsync(tx.Id, ct))
            {
                _logger.LogInformation(" Tx {TxId} ya fue procesada (idempotente)", tx.Id);
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
                            var dto = tx.Payload is null ? null : JsonSerializer.Deserialize<MateriaRequestDto>(tx.Payload, opts);
                            if (dto is null)
                            {
                                Skip(tx, "Payload vacío para POST/Materia");
                                return;
                            }

                            var nivelExiste = await _db.Niveles.AnyAsync(n => n.Id == dto.NivelId, ct);
                            if (!nivelExiste)
                            {
                                Skip(tx, $"El NivelId {dto.NivelId} no existe.");
                                return;
                            }

                            var yaExiste = await _db.Materias.AnyAsync(m => m.Codigo == dto.Codigo, ct);
                            if (yaExiste)
                            {
                                Skip(tx, $"Ya existe una materia con el código '{dto.Codigo}'.");
                                return;
                            }

                            var materia = new Materia
                            {
                                Codigo = dto.Codigo,
                                Nombre = dto.Nombre,
                                Creditos = dto.Creditos,
                                NivelId = dto.NivelId
                            };

                            _db.Materias.Add(materia);
                            break;
                        }   

                    case "PUT":
{
    var dto = tx.Payload is null ? null : JsonSerializer.Deserialize<MateriaRequestDto>(tx.Payload, opts);
    if (dto is null || string.IsNullOrWhiteSpace(dto.Codigo))
    {
        Skip(tx, "Payload vacío o sin Código para PUT/Materia");
        return;
    }

    var existente = await _db.Materias.FirstOrDefaultAsync(m => m.Codigo == dto.Codigo, ct);
    if (existente is null)
    {
        Skip(tx, $"Materia con Código '{dto.Codigo}' no existe");
        return;
    }

    var nivelExiste = await _db.Niveles.AnyAsync(n => n.Id == dto.NivelId, ct);
    if (!nivelExiste)
    {
        Skip(tx, $"El NivelId {dto.NivelId} no existe.");
        return;
    }

    existente.Nombre = dto.Nombre;
    existente.Creditos = dto.Creditos;
    existente.NivelId = dto.NivelId;
    break;
}

                    case "DELETE":
{
    var payload = JsonSerializer.Deserialize<Dictionary<string, string>>(tx.Payload ?? "{}", opts);
    if (!payload.TryGetValue("Codigo", out var codigo) || string.IsNullOrWhiteSpace(codigo))
    {
        Skip(tx, "Payload vacío o sin Código para DELETE/Materia");
        return;
    }

    var entity = await _db.Materias.FirstOrDefaultAsync(m => m.Codigo == codigo, ct);
    if (entity is null)
    {
        Skip(tx, $"Materia con Código '{codigo}' no existe, se omite la eliminación.");
        await _guard.MarkProcessedAsync(tx.Id, ct);
        tx.Estado = "COMPLETADO";
        return;
    }

    _db.Materias.Remove(entity);
    break;
}
} // <--- Fin del switch


                _logger.LogInformation(" Guardando cambios para Tx {TxId}", tx.Id);
                await _db.SaveChangesAsync(ct);

                await _guard.MarkProcessedAsync(tx.Id, ct);
                tx.Estado = "COMPLETADO";
            }
            catch (DbUpdateException ex) when ((ex.InnerException?.Message ?? ex.Message).Contains("foreign key", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError(ex, " Error de FK en Tx {TxId}: {Msg}", tx.Id, ex.Message);
                Skip(tx, $"No se puede procesar la operación en Materia: tiene dependencias (FK).");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, " Error de deserialización en Tx {TxId}", tx.Id);
                Skip(tx, "El payload de la transacción no es un JSON válido para Materia.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Excepción inesperada en Tx {TxId}: {Msg}", tx.Id, ex.Message);
                throw new InvalidOperationException($"Error inesperado en MateriaProcessor: {ex.Message}", ex);
            }
        }

        private void Skip(Transaccion tx, string motivo)
        {
            tx.Estado = "SKIP";
            _logger.LogWarning(" Tx {TxId} marcada como SKIP: {Motivo}", tx.Id, motivo);
        }
    }
}
