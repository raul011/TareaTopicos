// tareatopicos.ServicioA/Services/BackgroundTaskQueue.cs
using System.Collections.Concurrent;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Services;

public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly ConcurrentQueue<Transaccion> _cola = new();

    public void Enqueue(Transaccion transaccion)
    {
        _cola.Enqueue(transaccion);
    }

    public bool TryDequeue(out Transaccion? transaccion)
    {
        return _cola.TryDequeue(out transaccion);
    }
}
