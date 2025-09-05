using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Data;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;

namespace TAREATOPICOS.ServicioA.Services;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IBackgroundTaskQueue _queue;        
    private readonly ITransaccionStore _store;           
    private readonly IServiceProvider _serviceProvider;  

    public Worker(ILogger<Worker> logger,
                  IBackgroundTaskQueue queue,
                  ITransaccionStore store,
                  IServiceProvider serviceProvider)
    {
        _logger = logger;
        _queue = queue;
        _store = store;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker iniciado");
        const string ModelsNs = "TAREATOPICOS.ServicioA.Models.";

        while (!stoppingToken.IsCancellationRequested)
        {
            //saca la tarea de la cola
            var transaccion = await _queue.TryDequeueAsync(stoppingToken);

            if (transaccion != null)
            {
                try
                {   //actualizamos el estado 
                    await _store.UpdateEstadoAsync(transaccion.Id, "PROCESANDO");
                    await Task.Delay(1000, stoppingToken); // detenemos el proceso 

                    using var scope = _serviceProvider.CreateScope();
                    //creamos una cajita fuera de la cola para realizar el trabajo
                    var db = scope.ServiceProvider.GetRequiredService<ServicioAContext>();

                    var type = Type.GetType(ModelsNs + transaccion.Entidad)
                               ?? throw new InvalidOperationException($"Tipo no encontrado: {ModelsNs}{transaccion.Entidad}");

                    switch (transaccion.TipoOperacion.ToUpper())
                    {
                        case "POST":
                            var entityPost = JsonSerializer.Deserialize(transaccion.Payload!, type);
                            db.Add(entityPost!);
                            break;

                        case "PUT":
                            var entityPut = JsonSerializer.Deserialize(transaccion.Payload!, type);
                            db.Update(entityPut!);
                            break;

                        case "DELETE":
                            var json = JsonNode.Parse(transaccion.Payload!)!;
                            if (json["Id"] == null)
                                throw new InvalidOperationException("DELETE requiere un campo {Id}");

                            var idProp = type.GetProperty("Id")
                                       ?? throw new InvalidOperationException("El modelo no tiene propiedad Id");

                            var stub = Activator.CreateInstance(type)!;
                            idProp.SetValue(stub, json["Id"]!.GetValue<int>());
                            db.Attach(stub);
                            db.Remove(stub);
                            break;

                        default:
                            throw new InvalidOperationException($"Operación no soportada: {transaccion.TipoOperacion}");
                    }

                    await db.SaveChangesAsync(stoppingToken);
                    await _store.UpdateEstadoAsync(transaccion.Id, "COMPLETADO");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error procesando transacción {Id}", transaccion.Id);
                    await _store.UpdateEstadoAsync(transaccion.Id, "ERROR");
                }
            }
            else
            {
                await Task.Delay(500, stoppingToken);
            }
        }
    }
}
