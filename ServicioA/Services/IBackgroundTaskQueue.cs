// tareatopicos.ServicioA/Services/IBackgroundTaskQueue.cs
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Services;

public interface IBackgroundTaskQueue
{
    void Enqueue(Transaccion transaccion);
    bool TryDequeue(out Transaccion? transaccion);
}
