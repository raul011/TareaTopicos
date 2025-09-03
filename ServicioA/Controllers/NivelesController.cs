using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Services;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NivelesController : ControllerBase
{
    private readonly IBackgroundTaskQueue _queue; //encola la transaccion
    private readonly TransaccionStore _store; // almacena las transacciones

    public NivelesController(IBackgroundTaskQueue queue, TransaccionStore store)
    {
        _queue = queue;
        _store = store;
    }

    // post crearmos un nivel
    [HttpPost("async")]
    public IActionResult CrearNivelAsync([FromBody] Nivel nivel)
    {
        var transaccion = new Transaccion
        {
            TipoOperacion = "POST",
            Entidad = "Nivel",
            Payload = JsonSerializer.Serialize(nivel)
        };

        _store.Add(transaccion);
        _queue.Enqueue(transaccion);

       // devuelve el id de la transaccion y el estado inicial
        return Ok(new { id = transaccion.Id, estado = transaccion.Estado });
    }

    //  
    [HttpPut("async/{id:int}")]
    public IActionResult ActualizarNivelAsync(int id, [FromBody] Nivel nivel)
    {
        nivel.Id = id;  

        var transaccion = new Transaccion
        {
            TipoOperacion = "PUT",
            Entidad = "Nivel",
            Payload = JsonSerializer.Serialize(nivel)
        };

        _store.Add(transaccion);
        _queue.Enqueue(transaccion);

        return Ok(new { id = transaccion.Id, estado = transaccion.Estado });
    }

    // DELETE 
    [HttpDelete("async/{id:int}")]
    public IActionResult EliminarNivelAsync(int id)
    {
        var transaccion = new Transaccion
        {
            TipoOperacion = "DELETE",
            Entidad = "Nivel",
            Payload = JsonSerializer.Serialize(new { Id = id })
        };

        _store.Add(transaccion);
        _queue.Enqueue(transaccion);

        return Ok(new { id = transaccion.Id, estado = transaccion.Estado });
    }

     
    [HttpGet("estado/{id:guid}")]
    public IActionResult GetEstado(Guid id)
    {
        var transaccion = _store.Get(id);
        if (transaccion == null)
            return NotFound(new { mensaje = "Transacción no encontrada" });

        return Ok(new { id = transaccion.Id, estado = transaccion.Estado });
    }
}
