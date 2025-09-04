using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TAREATOPICOS.ServicioA.Dtos; // Necesario para DTOs si se usan en Payload
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Services;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NivelesController : ControllerBase
{
    private readonly IBackgroundTaskQueue _queue;
    private readonly RedisTransaccionStore _store;

    public NivelesController(IBackgroundTaskQueue queue, RedisTransaccionStore store)
    {
        _queue = queue;
        _store = store;
    }

    // POST asincrónico → crear nivel
    [HttpPost("async")]
    public async Task<IActionResult> CrearNivelAsync([FromBody] Nivel nivel)
    {
        var transaccion = new Transaccion
        {
            TipoOperacion = "POST",
            Entidad = "Nivel",
            Payload = JsonSerializer.Serialize(nivel)
        };

        await _store.AddAsync(transaccion);
        _queue.Enqueue(transaccion);

        // Devolvemos el ID de la transacción para futura consulta de estado
        return AcceptedAtAction("GetEstado", "Transacciones", new { id = transaccion.Id }, new { id = transaccion.Id, estado = transaccion.Estado });
    }

    // PUT asincrónico → actualizar nivel
    [HttpPut("async/{id:int}")]
    public async Task<IActionResult> ActualizarNivelAsync(int id, [FromBody] Nivel nivel)
    {
        nivel.Id = id; // el ID real del Nivel (int)

        var transaccion = new Transaccion
        {
            TipoOperacion = "PUT",
            Entidad = "Nivel",
            Payload = JsonSerializer.Serialize(nivel)
        };

        await _store.AddAsync(transaccion);
        _queue.Enqueue(transaccion);

        return AcceptedAtAction("GetEstado", "Transacciones", new { id = transaccion.Id }, new { id = transaccion.Id, estado = transaccion.Estado });
    }

    // DELETE asincrónico → eliminar nivel
    [HttpDelete("async/{id:int}")]
    public async Task<IActionResult> EliminarNivelAsync(int id)
    {
        var transaccion = new Transaccion
        {
            TipoOperacion = "DELETE",
            Entidad = "Nivel",
            Payload = JsonSerializer.Serialize(new { Id = id })
        };

        await _store.AddAsync(transaccion);
        _queue.Enqueue(transaccion);

        return AcceptedAtAction("GetEstado", "Transacciones", new { id = transaccion.Id }, new { id = transaccion.Id, estado = transaccion.Estado });
    }
}
