using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos.request;

namespace TAREATOPICOS.ServicioA.Services.Processors
{
    public class GrupoMateriaProcessor : IProcessor
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<GrupoMateriaProcessor> _logger;

        public GrupoMateriaProcessor(IServiceScopeFactory scopeFactory, ILogger<GrupoMateriaProcessor> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task ProcessAsync(Transaccion tx, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ServicioAContext>();
            var dto = JsonSerializer.Deserialize<GrupoMateriaRequestDto>(tx.Payload);

            if (dto is null)
            {
                throw new InvalidOperationException("Payload de GrupoMateria inválido.");
            }

            switch (tx.TipoOperacion)
            {
                case "CREATE":
                    var entity = new GrupoMateria
                    {
                        Grupo = dto.Grupo,
                        Cupo = dto.Cupo,
                        Estado = string.IsNullOrWhiteSpace(dto.Estado) ? "ACTIVO" : dto.Estado,
                        MateriaId = dto.MateriaId,
                        DocenteId = dto.DocenteId,
                        PeriodoId = dto.PeriodoId,
                        HorarioId = dto.HorarioId,
                        AulaId = dto.AulaId
                    };
                    context.GruposMaterias.Add(entity);
                    _logger.LogInformation("Creando nuevo GrupoMateria para MateriaId {MateriaId}", dto.MateriaId);
                    break;

                case "UPDATE":
                    var grupoToUpdate = await context.GruposMaterias.FirstOrDefaultAsync(g => g.Id == dto.Id, ct);
                    if (grupoToUpdate is null) return;

                    grupoToUpdate.Grupo = dto.Grupo;
                    grupoToUpdate.Cupo = dto.Cupo;
                    grupoToUpdate.Estado = dto.Estado;
                    grupoToUpdate.MateriaId = dto.MateriaId;
                    grupoToUpdate.DocenteId = dto.DocenteId;
                    grupoToUpdate.PeriodoId = dto.PeriodoId;
                    grupoToUpdate.HorarioId = dto.HorarioId;
                    grupoToUpdate.AulaId = dto.AulaId;
                    _logger.LogInformation("Actualizando GrupoMateria con Id {Id}", dto.Id);
                    break;

                case "DELETE":
                    var grupoToDelete = await context.GruposMaterias.FirstOrDefaultAsync(g => g.Id == dto.Id, ct);
                    if (grupoToDelete is null) return;
                    context.GruposMaterias.Remove(grupoToDelete);
                    _logger.LogInformation("Eliminando GrupoMateria con Id {Id}", dto.Id);
                    break;

                default:
                    _logger.LogWarning("Operación no soportada '{TipoOperacion}' para la entidad GrupoMateria.", tx.TipoOperacion);
                    throw new NotSupportedException($"Operación no soportada: {tx.TipoOperacion}");
            }

            try
            {
                await context.SaveChangesAsync(ct);
                _logger.LogInformation("Operación {TipoOperacion} para GrupoMateria completada para transacción {TransactionId}", tx.TipoOperacion, tx.Id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de base de datos al procesar transacción {TransactionId} para GrupoMateria.", tx.Id);
                throw;
            }
        }
    }
}