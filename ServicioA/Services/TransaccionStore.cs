using System.Collections.Concurrent;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Services;

public class TransaccionStore
{
    private readonly ConcurrentDictionary<Guid, Transaccion> _transacciones = new();

    public void Add(Transaccion transaccion)
    {
        _transacciones[transaccion.Id] = transaccion;
    }

    public Transaccion? Get(Guid id)
    {
        _transacciones.TryGetValue(id, out var transaccion);
        return transaccion;
    }

    public void UpdateEstado(Guid id, string nuevoEstado)
    {
        if (_transacciones.TryGetValue(id, out var transaccion))
        {
            transaccion.Estado = nuevoEstado;
        }
    }
        public IEnumerable<Transaccion> GetAll()
    {
        return _transacciones.Values;
    }
}
