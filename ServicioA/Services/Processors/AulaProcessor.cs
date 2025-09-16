using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos;

namespace TAREATOPICOS.ServicioA.Services.Processors
{
    public class AulaProcessor : IProcessor
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AulaProcessor> _logger;

        public AulaProcessor(IServiceScopeFactory scopeFactory, ILogger<AulaProcessor> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task ProcessAsync(Transaccion tx, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ServicioAContext>();
            var dto = JsonSerializer.Deserialize<AulaDto>(tx.Payload);

            if (dto is null)
            {
                throw new InvalidOperationException("Payload de Aula inválido.");
            }

            switch (tx.TipoOperacion)
            {
                case "CREATE":
                    var entity = new Aula
                    {
                        Codigo = dto.Codigo,
                        Capacidad = dto.Capacidad,
                        Ubicacion = dto.Ubicacion
                    };
                    context.Aulas.Add(entity);
                    _logger.LogInformation("Creando nueva Aula con Código {Codigo}", dto.Codigo);
                    break;

                case "UPDATE":
                    var aulaToUpdate = await context.Aulas.FirstOrDefaultAsync(a => a.Id == dto.Id, ct);
                    if (aulaToUpdate is null)
                    {
                        _logger.LogWarning("No se encontró Aula con Id {Id} para actualizar.", dto.Id);
                        return;
                    }
                    aulaToUpdate.Codigo = dto.Codigo;
                    aulaToUpdate.Capacidad = dto.Capacidad;
                    aulaToUpdate.Ubicacion = dto.Ubicacion;
                    _logger.LogInformation("Actualizando Aula con Id {Id}", dto.Id);
                    break;

                case "DELETE":
                    var aulaToDelete = await context.Aulas.FirstOrDefaultAsync(a => a.Id == dto.Id, ct);
                    if (aulaToDelete is null)
                    {
                        _logger.LogWarning("No se encontró Aula con Id {Id} para eliminar.", dto.Id);
                        return;
                    }
                    context.Aulas.Remove(aulaToDelete);
                    _logger.LogInformation("Eliminando Aula con Id {Id}", dto.Id);
                    break;

                default:
                    _logger.LogWarning("Operación no soportada '{TipoOperacion}' para la entidad Aula.", tx.TipoOperacion);
                    throw new NotSupportedException($"Operación no soportada: {tx.TipoOperacion}");
            }

            try
            {
                await context.SaveChangesAsync(ct);
                _logger.LogInformation("Operación {TipoOperacion} para Aula completada para transacción {TransactionId}", tx.TipoOperacion, tx.Id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de base de datos al procesar transacción {TransactionId} para Aula.", tx.Id);
                throw;
            }
        }
    }
}