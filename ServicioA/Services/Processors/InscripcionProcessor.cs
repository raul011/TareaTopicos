using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos.request;

namespace TAREATOPICOS.ServicioA.Services.Processors
{
    public class InscripcionProcessor : IProcessor
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<InscripcionProcessor> _logger;

        public InscripcionProcessor(IServiceScopeFactory scopeFactory, ILogger<InscripcionProcessor> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task ProcessAsync(Transaccion tx, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ServicioAContext>();

            switch (tx.TipoOperacion)
            {
                case "CREATE":
                    await HandleCreate(tx, context, ct);
                    break;
                case "FINALIZE":
                    await HandleFinalize(tx, context, ct);
                    break;
                default:
                    _logger.LogWarning("Operación no soportada '{TipoOperacion}' para la entidad Inscripcion.", tx.TipoOperacion);
                    throw new NotSupportedException($"Operación no soportada: {tx.TipoOperacion}");
            }

            try
            {
                await context.SaveChangesAsync(ct);
                _logger.LogInformation("Operación {TipoOperacion} para Inscripcion completada para transacción {TransactionId}", tx.TipoOperacion, tx.Id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de base de datos al procesar transacción {TransactionId} para Inscripcion.", tx.Id);
                throw;
            }
        }

        private async Task HandleCreate(Transaccion tx, ServicioAContext context, CancellationToken ct)
        {
            var dto = JsonSerializer.Deserialize<InscripcionRequestDto>(tx.Payload);
            if (dto is null) throw new InvalidOperationException("Payload de Inscripcion (CREATE) inválido.");

            var existeEst = await context.Estudiantes.AnyAsync(e => e.Id == dto.EstudianteId, ct);
            var existePer = await context.PeriodosAcademicos.AnyAsync(p => p.Id == dto.PeriodoId, ct);
            if (!existeEst || !existePer) throw new InvalidOperationException("Estudiante o Periodo inválido.");

            var duplicada = await context.Inscripciones.AnyAsync(i => i.EstudianteId == dto.EstudianteId && i.PeriodoId == dto.PeriodoId, ct);
            if (duplicada)
            {
                _logger.LogInformation("Inscripción duplicada para EstudianteId {EstudianteId} en PeriodoId {PeriodoId}. Se ignora la transacción.", dto.EstudianteId, dto.PeriodoId);
                return;
            }

            var entity = new Inscripcion
            {
                Fecha = dto.Fecha == default ? DateTime.UtcNow : dto.Fecha,
                Estado = string.IsNullOrWhiteSpace(dto.Estado) ? "PENDIENTE" : dto.Estado,
                EstudianteId = dto.EstudianteId,
                PeriodoId = dto.PeriodoId
            };
            context.Inscripciones.Add(entity);
        }

        private async Task HandleFinalize(Transaccion tx, ServicioAContext context, CancellationToken ct)
        {
            var dto = JsonSerializer.Deserialize<JsonElement>(tx.Payload);
            if (!dto.TryGetProperty("InscripcionId", out var idElement) || !idElement.TryGetInt32(out var inscripcionId))
            {
                throw new InvalidOperationException("Payload de Inscripcion (FINALIZE) inválido.");
            }

            var insc = await context.Inscripciones.Include(i => i.Detalles).FirstOrDefaultAsync(i => i.Id == inscripcionId, ct);
            if (insc is null) return;
            if (!insc.Detalles.Any()) return;

            insc.Estado = "FINALIZADA";
        }
    }
}