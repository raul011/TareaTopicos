using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Services;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NivelesController : ControllerBase
{
    private readonly IBackgroundTaskQueue _queue;
    private readonly TransaccionStore _store;

    public NivelesController(IBackgroundTaskQueue queue, TransaccionStore store)
    {
        _queue = queue;
        _store = store;
    }

    // POST asincrónico → crear nivel
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

        // 👇 devolvemos el ID de la transacción (Guid)
        return Ok(new { id = transaccion.Id, estado = transaccion.Estado });
    }

    // PUT asincrónico → actualizar nivel
    [HttpPut("async/{id:int}")]
    public IActionResult ActualizarNivelAsync(int id, [FromBody] Nivel nivel)
    {
        nivel.Id = id; // el ID real del Nivel (int)

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

    // DELETE asincrónico → eliminar nivel
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

    // Consultar estado de una transacción
    [HttpGet("estado/{id:guid}")]
    public IActionResult GetEstado(Guid id)
    {
        var transaccion = _store.Get(id);
        if (transaccion == null)
            return NotFound(new { mensaje = "Transacción no encontrada" });

        return Ok(new { id = transaccion.Id, estado = transaccion.Estado });
    }
}
