using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Data;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace TAREATOPICOS.ServicioA.Services;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IBackgroundTaskQueue _queue;
    private readonly TransaccionStore _store;
    private readonly IServiceProvider _serviceProvider;

    public Worker(ILogger<Worker> logger,
                  IBackgroundTaskQueue queue,
                  TransaccionStore store,
                  IServiceProvider serviceProvider)
    {
        _logger = logger;
        _queue = queue;
        _store = store;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker iniciado ");

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_queue.TryDequeue(out var transaccion) && transaccion != null)
            {
                try
                {
                    _store.UpdateEstado(transaccion.Id, "PROCESANDO");

                    await Task.Delay(2000, stoppingToken);

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var db = scope.ServiceProvider.GetRequiredService<ServicioAContext>();

                        switch (transaccion.TipoOperacion.ToUpper())
                        {
                            // ==================== POST ====================
                            case "POST":
                                if (transaccion.Entidad == "Nivel" && transaccion.Payload != null)
                                {
                                    var nivel = JsonSerializer.Deserialize<Nivel>(transaccion.Payload.ToString()!);
                                    if (nivel != null)
                                    {
                                        db.Niveles.Add(nivel);
                                        await db.SaveChangesAsync(stoppingToken);
                                        _logger.LogInformation("Nivel creado → {Nombre}", nivel.Nombre);
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Payload inválido en POST para {Entidad}", transaccion.Entidad);
                                    }
                                }
                                break;

                            // ==================== PUT ====================
                            case "PUT":
                                if (transaccion.Entidad == "Nivel" && transaccion.Payload != null)
                                {
                                    var nivel = JsonSerializer.Deserialize<Nivel>(transaccion.Payload.ToString()!);
                                    if (nivel != null)
                                    {
                                        var existente = await db.Niveles.FindAsync(new object[] { nivel.Id }, stoppingToken);
                                        if (existente != null)
                                        {
                                            existente.Nombre = nivel.Nombre;
                                            existente.Numero = nivel.Numero;
                                            await db.SaveChangesAsync(stoppingToken);
                                            _logger.LogInformation("Nivel actualizado → {Id}", nivel.Id);
                                        }
                                        else
                                        {
                                            _logger.LogWarning("Intento de actualizar Nivel {Id}, pero no existe ❌", nivel.Id);
                                        }
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Payload inválido en PUT para {Entidad}", transaccion.Entidad);
                                    }
                                }
                                break;

                            // ==================== DELETE ====================
                            case "DELETE":
                                if (transaccion.Entidad == "Nivel" && transaccion.Payload != null)
                                {
                                    try
                                    {
                                        // El payload puede ser solo { Id = x }
                                        var jsonDoc = JsonDocument.Parse(transaccion.Payload.ToString()!);
                                        if (jsonDoc.RootElement.TryGetProperty("Id", out var idProp))
                                        {
                                            var id = idProp.GetInt32();
                                            var existente = await db.Niveles.FindAsync(new object[] { id }, stoppingToken);
                                            if (existente != null)
                                            {
                                                db.Niveles.Remove(existente);
                                                await db.SaveChangesAsync(stoppingToken);
                                                _logger.LogInformation("Nivel {Id} eliminado correctamente ✅", id);
                                            }
                                            else
                                            {
                                                _logger.LogWarning("Intento de eliminar Nivel {Id}, pero no existe ❌", id);
                                            }
                                        }
                                        else
                                        {
                                            _logger.LogWarning("Payload de DELETE no contenía 'Id'");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, "Error interpretando payload de DELETE");
                                    }
                                }
                                break;

                            // ==================== GET (opcional) ====================
                            case "GET":
                                _logger.LogInformation("GET en cola aún no implementado para {Entidad}", transaccion.Entidad);
                                break;
                        }
                    }

                    // Siempre que no haya excepción real → marcar como COMPLETADO
                    _store.UpdateEstado(transaccion.Id, "COMPLETADO");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error procesando transacción {Id}", transaccion.Id);
                    _store.UpdateEstado(transaccion.Id, "ERROR");
                }
            }
            else
            {
                await Task.Delay(500, stoppingToken); // espera medio segundo si no hay tareas
            }
        }
    }
}
