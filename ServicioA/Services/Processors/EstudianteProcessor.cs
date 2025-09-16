using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos;

namespace TAREATOPICOS.ServicioA.Services.Processors
{
    public class EstudianteProcessor : IProcessor
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EstudianteProcessor> _logger;

        public EstudianteProcessor(IServiceScopeFactory scopeFactory, ILogger<EstudianteProcessor> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task ProcessAsync(Transaccion tx, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ServicioAContext>();
            var dto = JsonSerializer.Deserialize<EstudianteRequestDto>(tx.Payload);

            if (dto is null)
            {
                throw new InvalidOperationException("Payload de Estudiante inválido.");
            }

            switch (tx.TipoOperacion)
            {
                case "CREATE":
                    var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
                    var entity = new Estudiante
                    {
                        Registro = dto.Registro,
                        Ci = dto.Ci,
                        Nombre = dto.Nombre,
                        Email = dto.Email,
                        Telefono = dto.Telefono,
                        Direccion = dto.Direccion,
                        Estado = string.IsNullOrWhiteSpace(dto.Estado) ? "ACTIVO" : dto.Estado,
                        CarreraId = dto.CarreraId,
                        PasswordHash = passwordHash
                    };
                    context.Estudiantes.Add(entity);
                    _logger.LogInformation("Creando nuevo Estudiante con Registro {Registro}", dto.Registro);
                    break;

                case "UPDATE":
                    var estudianteToUpdate = await context.Estudiantes.FirstOrDefaultAsync(e => e.Id == dto.Id, ct);
                    if (estudianteToUpdate is null)
                    {
                        _logger.LogWarning("No se encontró Estudiante con Id {Id} para actualizar.", dto.Id);
                        return;
                    }
                    estudianteToUpdate.Registro = dto.Registro;
                    estudianteToUpdate.Ci = dto.Ci;
                    estudianteToUpdate.Nombre = dto.Nombre;
                    estudianteToUpdate.Email = dto.Email;
                    estudianteToUpdate.Telefono = dto.Telefono;
                    estudianteToUpdate.Direccion = dto.Direccion;
                    estudianteToUpdate.Estado = dto.Estado;
                    estudianteToUpdate.CarreraId = dto.CarreraId;

                    if (!string.IsNullOrEmpty(dto.Password))
                    {
                        estudianteToUpdate.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
                    }
                    _logger.LogInformation("Actualizando Estudiante con Id {Id}", dto.Id);
                    break;

                case "DELETE":
                    var estudianteToDelete = await context.Estudiantes.FirstOrDefaultAsync(e => e.Id == dto.Id, ct);
                    if (estudianteToDelete is null)
                    {
                        _logger.LogWarning("No se encontró Estudiante con Id {Id} para eliminar.", dto.Id);
                        return;
                    }
                    context.Estudiantes.Remove(estudianteToDelete);
                    _logger.LogInformation("Eliminando Estudiante con Id {Id}", dto.Id);
                    break;

                default:
                    _logger.LogWarning("Operación no soportada '{TipoOperacion}' para la entidad Estudiante.", tx.TipoOperacion);
                    throw new NotSupportedException($"Operación no soportada: {tx.TipoOperacion}");
            }

            try
            {
                await context.SaveChangesAsync(ct);
                _logger.LogInformation("Operación {TipoOperacion} para Estudiante completada para transacción {TransactionId}", tx.TipoOperacion, tx.Id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de base de datos al procesar transacción {TransactionId} para Estudiante.", tx.Id);
                throw;
            }
        }
    }
}