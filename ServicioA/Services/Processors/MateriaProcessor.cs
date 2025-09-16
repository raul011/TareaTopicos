using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos.request;

namespace TAREATOPICOS.ServicioA.Services.Processors
{
    public class MateriaProcessor : IProcessor
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MateriaProcessor> _logger;

        public MateriaProcessor(IServiceScopeFactory scopeFactory, ILogger<MateriaProcessor> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task ProcessAsync(Transaccion tx, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ServicioAContext>();
            var dto = JsonSerializer.Deserialize<MateriaRequestDto>(tx.Payload);

            if (dto is null)
            {
                throw new InvalidOperationException("Payload de Materia inválido.");
            }

            switch (tx.TipoOperacion)
            {
                case "CREATE":
                    var entity = new Materia
                    {
                        Codigo = dto.Codigo,
                        Nombre = dto.Nombre,
                        Creditos = dto.Creditos,
                        NivelId = dto.NivelId
                    };
                    context.Materias.Add(entity);
                    _logger.LogInformation("Creando nueva Materia con Código {Codigo}", dto.Codigo);
                    break;

                case "UPDATE":
                    var materiaToUpdate = await context.Materias.FirstOrDefaultAsync(m => m.Id == dto.Id, ct);
                    if (materiaToUpdate is null)
                    {
                        _logger.LogWarning("No se encontró Materia con Id {Id} para actualizar.", dto.Id);
                        return;
                    }
                    materiaToUpdate.Nombre = dto.Nombre;
                    materiaToUpdate.Creditos = dto.Creditos;
                    materiaToUpdate.NivelId = dto.NivelId;
                    _logger.LogInformation("Actualizando Materia con Id {Id}", dto.Id);
                    break;

                case "DELETE":
                    var materiaToDelete = await context.Materias.FirstOrDefaultAsync(m => m.Id == dto.Id, ct);
                    if (materiaToDelete is null)
                    {
                        _logger.LogWarning("No se encontró Materia con Id {Id} para eliminar.", dto.Id);
                        return;
                    }
                    context.Materias.Remove(materiaToDelete);
                    _logger.LogInformation("Eliminando Materia con Id {Id}", dto.Id);
                    break;

                default:
                    _logger.LogWarning("Operación no soportada '{TipoOperacion}' para la entidad Materia.", tx.TipoOperacion);
                    throw new NotSupportedException($"Operación no soportada: {tx.TipoOperacion}");
            }

            try
            {
                await context.SaveChangesAsync(ct);
                _logger.LogInformation("Operación {TipoOperacion} para Materia completada para transacción {TransactionId}", tx.TipoOperacion, tx.Id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de base de datos al procesar transacción {TransactionId} para Materia.", tx.Id);
                throw;
            }
        }
    }
}