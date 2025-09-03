// tareatopicos.ServicioA/Services/BackgroundTaskQueue.cs
using System.Collections.Concurrent;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Services;

public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly ConcurrentQueue<Transaccion> _cola = new();

    public void Enqueue(Transaccion transaccion) //agrega al final de la cola
    {
        _cola.Enqueue(transaccion);
    }

    public bool TryDequeue(out Transaccion? transaccion) //saca una transaccion del inicio de la cola
    {
        return _cola.TryDequeue(out transaccion);
    }
}
//implementa la cola en memoria para alamacenar las transacciones aqui se acumulan las transacciones