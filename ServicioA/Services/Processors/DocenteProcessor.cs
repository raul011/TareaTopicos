using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos;

namespace TAREATOPICOS.ServicioA.Services.Processors
{
    public class DocenteProcessor : IProcessor
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DocenteProcessor> _logger;

        public DocenteProcessor(IServiceScopeFactory scopeFactory, ILogger<DocenteProcessor> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task ProcessAsync(Transaccion tx, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ServicioAContext>();
            var dto = JsonSerializer.Deserialize<DocenteDto>(tx.Payload);

            if (dto is null)
            {
                throw new InvalidOperationException("Payload de Docente inválido.");
            }

            switch (tx.TipoOperacion)
            {
                case "CREATE":
                    var entity = new Docente
                    {
                        Registro = dto.Registro,
                        Ci = dto.Ci,
                        Nombre = dto.Nombre,
                        Telefono = dto.Telefono,
                        Estado = string.IsNullOrWhiteSpace(dto.Estado) ? "ACTIVO" : dto.Estado
                    };
                    context.Docentes.Add(entity);
                    _logger.LogInformation("Creando nuevo Docente con Registro {Registro}", dto.Registro);
                    break;

                case "UPDATE":
                    var docenteToUpdate = await context.Docentes.FirstOrDefaultAsync(d => d.Id == dto.Id, ct);
                    if (docenteToUpdate is null)
                    {
                        _logger.LogWarning("No se encontró Docente con Id {Id} para actualizar.", dto.Id);
                        return;
                    }
                    docenteToUpdate.Ci = dto.Ci;
                    docenteToUpdate.Nombre = dto.Nombre;
                    docenteToUpdate.Telefono = dto.Telefono;
                    docenteToUpdate.Estado = string.IsNullOrWhiteSpace(dto.Estado) ? docenteToUpdate.Estado : dto.Estado;
                    _logger.LogInformation("Actualizando Docente con Id {Id}", dto.Id);
                    break;

                case "DELETE":
                    var docenteToDelete = await context.Docentes.FirstOrDefaultAsync(d => d.Id == dto.Id, ct);
                    if (docenteToDelete is null)
                    {
                        _logger.LogWarning("No se encontró Docente con Id {Id} para eliminar.", dto.Id);
                        return;
                    }
                    context.Docentes.Remove(docenteToDelete);
                    _logger.LogInformation("Eliminando Docente con Id {Id}", dto.Id);
                    break;

                default:
                    _logger.LogWarning("Operación no soportada '{TipoOperacion}' para la entidad Docente.", tx.TipoOperacion);
                    throw new NotSupportedException($"Operación no soportada: {tx.TipoOperacion}");
            }

            try
            {
                await context.SaveChangesAsync(ct);
                _logger.LogInformation("Operación {TipoOperacion} para Docente completada para transacción {TransactionId}", tx.TipoOperacion, tx.Id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de base de datos al procesar transacción {TransactionId} para Docente.", tx.Id);
                throw;
            }
        }
    }
}