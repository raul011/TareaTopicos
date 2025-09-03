// tareatopicos.ServicioA/Services/IBackgroundTaskQueue.cs
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Services;

public interface IBackgroundTaskQueue
{
    void Enqueue(Transaccion transaccion); //mete  a la cola 
    bool TryDequeue(out Transaccion? transaccion);  //saca de la cola
}
//define que metodo debe implementar en la cola