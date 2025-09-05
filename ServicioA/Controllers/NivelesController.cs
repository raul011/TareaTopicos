using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Services;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NivelesController : ControllerBase
{
    private readonly IBackgroundTaskQueue _queue; // encola la transacción en redis
    private readonly ITransaccionStore _store;    // guarda/lee estado de la tarea

    public NivelesController(IBackgroundTaskQueue queue, ITransaccionStore store)
    {
        _queue = queue;
        _store = store;
    }

    //  /api/niveles/async  
    [HttpPost("async")]
    public async Task<IActionResult> CrearNivelAsync([FromBody] Nivel nivel, CancellationToken ct)
    {
        //empaqietamos
        var transaccion = new Transaccion
        {
            TipoOperacion = "POST",
            Entidad = "Nivel",
            Payload = JsonSerializer.Serialize(nivel), //serializamos
            Estado = "EN_COLA"
        };

        await _store.AddAsync(transaccion); //guardamos el estado inicial
        await _queue.EnqueueAsync(transaccion); //encolamos

        return Accepted(new { id = transaccion.Id, estado = transaccion.Estado });
    }

    //  /api/niveles/async/{id} 
    [HttpPut("async/{id:int}")]
    public async Task<IActionResult> ActualizarNivelAsync(int id, [FromBody] Nivel nivel, CancellationToken ct)
    {
        nivel.Id = id;

        var transaccion = new Transaccion
        {
            TipoOperacion = "PUT",
            Entidad = "Nivel",
            Payload = JsonSerializer.Serialize(nivel),
            Estado = "EN_COLA"
        };

        await _store.AddAsync(transaccion);
        await _queue.EnqueueAsync(transaccion);

        return Accepted(new { id = transaccion.Id, estado = transaccion.Estado });
    }

    //api/niveles/async/{id}  
    [HttpDelete("async/{id:int}")]
    public async Task<IActionResult> EliminarNivelAsync(int id, CancellationToken ct)
    {
        var transaccion = new Transaccion
        {
            TipoOperacion = "DELETE",
            Entidad = "Nivel",
            Payload = JsonSerializer.Serialize(new { Id = id }),
            Estado = "EN_COLA"
        };

        await _store.AddAsync(transaccion);
        await _queue.EnqueueAsync(transaccion);

        return Accepted(new { id = transaccion.Id, estado = transaccion.Estado });
    }

    //api/niveles/estado/{id} 
    [HttpGet("estado/{id:guid}")]
    public async Task<IActionResult> GetEstado(Guid id, CancellationToken ct)
    {
        var transaccion = await _store.GetAsync(id);
        if (transaccion is null)
            return NotFound(new { mensaje = "Transacción no encontrada" });

        return Ok(new { id = transaccion.Id, estado = transaccion.Estado });
    }
}
