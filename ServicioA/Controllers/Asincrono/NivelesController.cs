using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Services;

namespace TAREATOPICOS.ServicioA.Controllers.Asincrono;

[ApiController]
[Route("api/[controller]")]
public class NivelesController : ControllerBase
{
    private readonly QueueManager _qm;
    private readonly ITransaccionStore _store;
    private readonly ServicioAContext _db;
    private readonly IConfiguration _cfg;

    public NivelesController(QueueManager qm, ITransaccionStore store, ServicioAContext db, IConfiguration cfg)
    {
        _qm = qm;
        _store = store;
        _db = db;
        _cfg = cfg;
    }

    // POST /api/niveles/async
    [HttpPost("async")]
    public async Task<IActionResult> CrearNivelAsync(
        [FromBody] Nivel nivel,
        [FromQuery] string? queue = "default",
        [FromQuery] int priority = 1,
        [FromQuery] DateTimeOffset? notBeforeUtc = null,
        CancellationToken ct = default)
    {
        // Evita duplicar por Numero (índice único)
        if (await _db.Niveles.AnyAsync(x => x.Numero == nivel.Numero, ct))
            return Accepted(new { mensaje = $"Nivel ya existe (Numero {nivel.Numero} duplicado)" });

        var tx = new Transaccion
        {
            TipoOperacion = "POST",
            Entidad       = "Nivel",
            Payload       = JsonSerializer.Serialize(nivel),
            Estado        = "EN_COLA",
            Priority      = Math.Clamp(priority, 0, 2),
            NotBefore     = notBeforeUtc ?? DateTimeOffset.UtcNow
        };
        tx.CallbackUrl    ??= _cfg["Webhook:DefaultUrl"];
        tx.CallbackSecret ??= _cfg["Webhook:DefaultSecret"];
        tx.IdempotencyKey ??= tx.Id.ToString();

        await _qm.EnqueueAsync(tx, queue, ct);
        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    // PUT /api/niveles/async/numero/5077
    [HttpPut("async/numero/{numero:int}")]
    public async Task<IActionResult> ActualizarNivelPorNumeroAsync(
        int numero,
        [FromBody] Nivel nivel,                // el cliente NO manda Id
        [FromQuery] string? queue = "default",
        [FromQuery] int priority = 1,
        [FromQuery] DateTimeOffset? notBeforeUtc = null,
        CancellationToken ct = default)
    {
        nivel.Id = 0;                 // no exponer Id
        nivel.Numero = numero;        // forzar Numero desde ruta

        var tx = new Transaccion
        {
            TipoOperacion = "PUT",
            Entidad       = "Nivel",
            Payload       = JsonSerializer.Serialize(nivel),
            Estado        = "EN_COLA",
            Priority      = Math.Clamp(priority, 0, 2),
            NotBefore     = notBeforeUtc ?? DateTimeOffset.UtcNow
        };
        tx.CallbackUrl    ??= _cfg["Webhook:DefaultUrl"];
        tx.CallbackSecret ??= _cfg["Webhook:DefaultSecret"];
        tx.IdempotencyKey ??= tx.Id.ToString();

        await _qm.EnqueueAsync(tx, queue, ct);
        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    // DELETE /api/niveles/async/numero/5077
    [HttpDelete("async/numero/{numero:int}")]
    public async Task<IActionResult> EliminarNivelPorNumeroAsync(
        int numero,
        [FromQuery] string? queue = "default",
        [FromQuery] int priority = 1,
        [FromQuery] DateTimeOffset? notBeforeUtc = null,
        CancellationToken ct = default)
    {
        var payload = new { Numero = numero };

        var tx = new Transaccion
        {
            TipoOperacion = "DELETE",
            Entidad       = "Nivel",
            Payload       = JsonSerializer.Serialize(payload),
            Estado        = "EN_COLA",
            Priority      = Math.Clamp(priority, 0, 2),
            NotBefore     = notBeforeUtc ?? DateTimeOffset.UtcNow
        };
        tx.CallbackUrl    ??= _cfg["Webhook:DefaultUrl"];
        tx.CallbackSecret ??= _cfg["Webhook:DefaultSecret"];
        tx.IdempotencyKey ??= tx.Id.ToString();

        await _qm.EnqueueAsync(tx, queue, ct);
        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    // GET /api/niveles/estado/{id}
    [HttpGet("estado/{id:guid}")]
    public async Task<IActionResult> GetEstado(Guid id, CancellationToken ct = default)
    {
        var tx = await _store.GetAsync(id, ct);
        return tx is null
            ? NotFound(new { mensaje = "Transacción no encontrada" })
            : Ok(new { id = tx.Id, estado = tx.Estado });
    }

    /* ====== (Opcional) RUTAS ANTIGUAS POR Id – si aún las necesitas, deja esto; si no, bórralo ======

    // PUT /api/niveles/async/{id:int}
    [HttpPut("async/{id:int}")]
    public async Task<IActionResult> ActualizarNivelAsync(
        int id, [FromBody] Nivel nivel, [FromQuery] string? queue = "default",
        [FromQuery] int priority = 1, [FromQuery] DateTimeOffset? notBeforeUtc = null, CancellationToken ct = default)
    {
        nivel.Id = id;
        var tx = new Transaccion { TipoOperacion = "PUT", Entidad = "Nivel",
            Payload = JsonSerializer.Serialize(nivel), Estado = "EN_COLA",
            Priority = Math.Clamp(priority, 0, 2), NotBefore = notBeforeUtc ?? DateTimeOffset.UtcNow };
        await _qm.EnqueueAsync(tx, queue, ct);
        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    // DELETE /api/niveles/async/{id:int}
    [HttpDelete("async/{id:int}")]
    public async Task<IActionResult> EliminarNivelAsync(
        int id, [FromQuery] string? queue = "default",
        [FromQuery] int priority = 1, [FromQuery] DateTimeOffset? notBeforeUtc = null, CancellationToken ct = default)
    {
        var tx = new Transaccion { TipoOperacion = "DELETE", Entidad = "Nivel",
            Payload = JsonSerializer.Serialize(new { Id = id }), Estado = "EN_COLA",
            Priority = Math.Clamp(priority, 0, 2), NotBefore = notBeforeUtc ?? DateTimeOffset.UtcNow };
        await _qm.EnqueueAsync(tx, queue, ct);
        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }
    ================================================================================================== */
}

 
