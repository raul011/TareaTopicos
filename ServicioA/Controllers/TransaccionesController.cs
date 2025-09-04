using Microsoft.AspNetCore.Mvc;
using TAREATOPICOS.ServicioA.Services;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransaccionesController : ControllerBase
{
    private readonly RedisTransaccionStore _store;

    public TransaccionesController(RedisTransaccionStore store)
    {
        _store = store;
    }

    // GET api/transacciones/estado/{id}
    [HttpGet("estado/{id:guid}")]
    public async Task<IActionResult> GetEstado(Guid id)
    {
        var transaccion = await _store.GetByIdAsync(id);
        return transaccion == null
            ? NotFound(new { mensaje = "Transacción no encontrada" })
            : Ok(new { id = transaccion.Id, estado = transaccion.Estado });
    }

    // GET api/transacciones/estado
    [HttpGet("estado")]
    public async Task<IActionResult> GetAll()
    {
        var transacciones = await _store.GetAllAsync();
        return Ok(transacciones);
    }
}