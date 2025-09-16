using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos;

namespace TAREATOPICOS.ServicioA.Services.Processors
{
    public class PlanDeEstudioProcessor : IProcessor
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PlanDeEstudioProcessor> _logger;

        public PlanDeEstudioProcessor(IServiceScopeFactory scopeFactory, ILogger<PlanDeEstudioProcessor> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task ProcessAsync(Transaccion tx, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ServicioAContext>();
            var dto = JsonSerializer.Deserialize<PlanDeEstudioDto>(tx.Payload);

            if (dto is null)
            {
                throw new InvalidOperationException("Payload de PlanDeEstudio inválido.");
            }

            switch (tx.TipoOperacion)
            {
                case "CREATE":
                    var entity = new PlanDeEstudio
                    {
                        Nombre = dto.Nombre,
                        Codigo = dto.Codigo,
                        Fecha = dto.Fecha,
                        Estado = string.IsNullOrWhiteSpace(dto.Estado) ? "ACTIVO" : dto.Estado,
                        CarreraId = dto.CarreraId
                    };
                    context.PlanesEstudio.Add(entity);
                    _logger.LogInformation("Creando nuevo Plan de Estudio con Código {Codigo}", dto.Codigo);
                    break;

                case "UPDATE":
                    var planToUpdate = await context.PlanesEstudio.FirstOrDefaultAsync(p => p.Id == dto.Id, ct);
                    if (planToUpdate is null)
                    {
                        _logger.LogWarning("No se encontró Plan de Estudio con Id {Id} para actualizar.", dto.Id);
                        return;
                    }
                    planToUpdate.Nombre = dto.Nombre;
                    planToUpdate.Codigo = dto.Codigo;
                    planToUpdate.Fecha = dto.Fecha;
                    planToUpdate.Estado = dto.Estado;
                    planToUpdate.CarreraId = dto.CarreraId;
                    _logger.LogInformation("Actualizando Plan de Estudio con Id {Id}", dto.Id);
                    break;

                case "DELETE":
                    var planToDelete = await context.PlanesEstudio.FirstOrDefaultAsync(p => p.Id == dto.Id, ct);
                    if (planToDelete is null)
                    {
                        _logger.LogWarning("No se encontró Plan de Estudio con Id {Id} para eliminar.", dto.Id);
                        return;
                    }
                    context.PlanesEstudio.Remove(planToDelete);
                    _logger.LogInformation("Eliminando Plan de Estudio con Id {Id}", dto.Id);
                    break;

                default:
                    _logger.LogWarning("Operación no soportada '{TipoOperacion}' para la entidad PlanDeEstudio.", tx.TipoOperacion);
                    throw new NotSupportedException($"Operación no soportada: {tx.TipoOperacion}");
            }

            try
            {
                await context.SaveChangesAsync(ct);
                _logger.LogInformation("Operación {TipoOperacion} para PlanDeEstudio completada para transacción {TransactionId}", tx.TipoOperacion, tx.Id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de base de datos al procesar transacción {TransactionId} para PlanDeEstudio.", tx.Id);
                throw;
            }
        }
    }
}