// tareatopicos.ServicioA/Services/TransaccionStore.cs
using System.Collections.Concurrent;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Services;

public class TransaccionStore
{
    private readonly ConcurrentDictionary<Guid, Transaccion> _transacciones = new();

    public void Add(Transaccion transaccion) //guarda la transaccion
    {
        _transacciones[transaccion.Id] = transaccion;
    }

    public Transaccion? Get(Guid id)// permite consulta la transaccion por id
    {
        _transacciones.TryGetValue(id, out var transaccion);
        return transaccion;
    }

    public void UpdateEstado(Guid id, string nuevoEstado) // cambia estado de la cola
                                                          // 
    {
        if (_transacciones.TryGetValue(id, out var transaccion))
        {
            transaccion.Estado = nuevoEstado;
        }
    }
        public IEnumerable<Transaccion> GetAll() //devuelve todas las transacciones registradas
    {
        return _transacciones.Values;
    }
}
//guarda en memoria el estado de cada transaccion y puede conultar el estado por el id