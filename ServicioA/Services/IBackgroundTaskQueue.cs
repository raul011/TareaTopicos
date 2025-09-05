// tareatopicos.ServicioA/Services/IBackgroundTaskQueue.cs
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Services;

public interface IBackgroundTaskQueue
{
    Task EnqueueAsync(Transaccion transaccion);                // encolar
    Task<Transaccion?> TryDequeueAsync(CancellationToken ct);  // desencolar
}

//define que metodo debe implementar en la cola
// ordena as transaccion que el worker va procesar