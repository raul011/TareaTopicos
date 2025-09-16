using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos.request;

namespace TAREATOPICOS.ServicioA.Services.Processors
{
    public class PeriodoAcademicoProcessor : IProcessor
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PeriodoAcademicoProcessor> _logger;

        public PeriodoAcademicoProcessor(IServiceScopeFactory scopeFactory, ILogger<PeriodoAcademicoProcessor> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task ProcessAsync(Transaccion tx, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ServicioAContext>();
            var dto = JsonSerializer.Deserialize<PeriodoAcademicoRequestDto>(tx.Payload);

            if (dto is null)
            {
                throw new InvalidOperationException("Payload de PeriodoAcademico inválido.");
            }

            switch (tx.TipoOperacion)
            {
                case "CREATE":
                    var entity = new PeriodoAcademico
                    {
                        Gestion = dto.Gestion,
                        FechaInicio = dto.FechaInicio,
                        FechaFin = dto.FechaFin
                    };
                    context.PeriodosAcademicos.Add(entity);
                    _logger.LogInformation("Creando nuevo Periodo Académico con Gestión {Gestion}", dto.Gestion);
                    break;

                case "UPDATE":
                    var periodoToUpdate = await context.PeriodosAcademicos.FirstOrDefaultAsync(p => p.Id == dto.Id, ct);
                    if (periodoToUpdate is null)
                    {
                        _logger.LogWarning("No se encontró Periodo Académico con Id {Id} para actualizar.", dto.Id);
                        return;
                    }
                    periodoToUpdate.Gestion = dto.Gestion;
                    periodoToUpdate.FechaInicio = dto.FechaInicio;
                    periodoToUpdate.FechaFin = dto.FechaFin;
                    _logger.LogInformation("Actualizando Periodo Académico con Id {Id}", dto.Id);
                    break;

                case "DELETE":
                    var periodoToDelete = await context.PeriodosAcademicos.FirstOrDefaultAsync(p => p.Id == dto.Id, ct);
                    if (periodoToDelete is null)
                    {
                        _logger.LogWarning("No se encontró Periodo Académico con Id {Id} para eliminar.", dto.Id);
                        return;
                    }
                    context.PeriodosAcademicos.Remove(periodoToDelete);
                    _logger.LogInformation("Eliminando Periodo Académico con Id {Id}", dto.Id);
                    break;

                default:
                    _logger.LogWarning("Operación no soportada '{TipoOperacion}' para la entidad PeriodoAcademico.", tx.TipoOperacion);
                    throw new NotSupportedException($"Operación no soportada: {tx.TipoOperacion}");
            }

            try
            {
                await context.SaveChangesAsync(ct);
                _logger.LogInformation("Operación {TipoOperacion} para PeriodoAcademico completada para transacción {TransactionId}", tx.TipoOperacion, tx.Id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de base de datos al procesar transacción {TransactionId} para PeriodoAcademico.", tx.Id);
                throw;
            }
        }
    }
}