
//TAREATOPICOS/ServicioA/Services/ITransaccionStore.cs
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Services;

public interface ITransaccionStore
{
    Task AddAsync(Transaccion t);
    Task<Transaccion?> GetAsync(Guid id);
    Task UpdateEstadoAsync(Guid id, string estado);
    Task<IEnumerable<Transaccion>> GetAllAsync();  
}
// almacena los estados de las transacciones
// los metodos de las transacciones