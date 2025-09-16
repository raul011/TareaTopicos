using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos;
using TAREATOPICOS.ServicioA.Services;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AulasController : ControllerBase
{
    private readonly ServicioAContext _context;
    private readonly QueueManager _qm;
    private readonly ITransaccionStore _store;
    private readonly IConfiguration _cfg;

    public AulasController(ServicioAContext context, QueueManager qm, ITransaccionStore store, IConfiguration cfg)
    {
        _context = context;
        _qm = qm;
        _store = store;
        _cfg = cfg;
    }

    // === ENDPOINTS SÍNCRONOS ===

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AulaDto>>> Get(CancellationToken ct = default)
    {
        var items = await _context.Aulas
            .AsNoTracking()
            .OrderBy(a => a.Codigo)
            .ToListAsync(ct);

        return Ok(items.Select(ToDTO));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AulaDto>> GetById(int id, CancellationToken ct = default)
    {
        var aula = await _context.Aulas
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return aula is null ? NotFound() : Ok(ToDTO(aula));
    }

    [HttpPost]
    public async Task<ActionResult<AulaDto>> Create([FromBody] AulaDto dto, CancellationToken ct = default)
    {
        var entity = new Aula
        {
            Codigo = dto.Codigo,
            Capacidad = dto.Capacidad,
            Ubicacion = dto.Ubicacion
        };

        _context.Aulas.Add(entity);
        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ToDTO(entity));
    }

    [HttpPut("codigo/{codigo}")]
public async Task<IActionResult> UpdateByCodigo(string codigo, [FromBody] AulaDto dto, CancellationToken ct = default)
{
    var aula = await _context.Aulas.FirstOrDefaultAsync(a => a.Codigo == codigo, ct);
    if (aula is null) return NotFound();

    aula.Capacidad = dto.Capacidad;
    aula.Ubicacion = dto.Ubicacion;

    await _context.SaveChangesAsync(ct);
    return NoContent();
}

    [HttpDelete("codigo/{codigo}")]
public async Task<IActionResult> DeleteByCodigo(string codigo, CancellationToken ct = default)
{
    var aula = await _context.Aulas.FirstOrDefaultAsync(a => a.Codigo == codigo, ct);
    if (aula is null) return NotFound();

    _context.Aulas.Remove(aula);
    await _context.SaveChangesAsync(ct);
    return NoContent();
}

    // === ENDPOINTS ASÍNCRONOS ===

    [HttpPost("async")]
    public async Task<IActionResult> CrearAulaAsync(
        [FromBody] AulaDto dto,
        [FromQuery] string? queue = "default",
        [FromQuery] int priority = 1,
        [FromQuery] DateTimeOffset? notBeforeUtc = null,
        CancellationToken ct = default)
    {
        var tx = new Transaccion
        {
            TipoOperacion = "POST",
            Entidad = "Aula",
            Payload = JsonSerializer.Serialize(dto),
            Estado = "EN_COLA",
            Priority = Math.Clamp(priority, 0, 2),
            NotBefore = notBeforeUtc ?? DateTimeOffset.UtcNow,
            CallbackUrl = _cfg["Webhook:DefaultUrl"],
            CallbackSecret = _cfg["Webhook:DefaultSecret"],
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        await _qm.EnqueueAsync(tx, queue, ct);
        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    [HttpPut("async/codigo/{codigo}")]
public async Task<IActionResult> ActualizarAulaAsyncPorCodigo(
    string codigo,
    [FromBody] AulaDto dto,
    [FromQuery] string? queue = "default",
    [FromQuery] int priority = 1,
    [FromQuery] DateTimeOffset? notBeforeUtc = null,
    CancellationToken ct = default)
{
    dto.Codigo = codigo;

    var tx = new Transaccion
    {
        TipoOperacion = "PUT",
        Entidad = "Aula",
        Payload = JsonSerializer.Serialize(dto),
        Estado = "EN_COLA",
        Priority = Math.Clamp(priority, 0, 2),
        NotBefore = notBeforeUtc ?? DateTimeOffset.UtcNow,
        CallbackUrl = _cfg["Webhook:DefaultUrl"],
        CallbackSecret = _cfg["Webhook:DefaultSecret"],
        IdempotencyKey = Guid.NewGuid().ToString()
    };

    await _qm.EnqueueAsync(tx, queue, ct);
    return Accepted(new { id = tx.Id, estado = tx.Estado });
}

    [HttpDelete("async/codigo/{codigo}")]
public async Task<IActionResult> EliminarAulaAsyncPorCodigo(
    string codigo,
    [FromQuery] string? queue = "default",
    [FromQuery] int priority = 1,
    [FromQuery] DateTimeOffset? notBeforeUtc = null,
    CancellationToken ct = default)
{
    var payload = new { Codigo = codigo };

    var tx = new Transaccion
    {
        TipoOperacion = "DELETE",
        Entidad = "Aula",
        Payload = JsonSerializer.Serialize(payload),
        Estado = "EN_COLA",
        Priority = Math.Clamp(priority, 0, 2),
        NotBefore = notBeforeUtc ?? DateTimeOffset.UtcNow,
        CallbackUrl = _cfg["Webhook:DefaultUrl"],
        CallbackSecret = _cfg["Webhook:DefaultSecret"],
        IdempotencyKey = Guid.NewGuid().ToString()
    };

    await _qm.EnqueueAsync(tx, queue, ct);
    return Accepted(new { id = tx.Id, estado = tx.Estado });
}

    [HttpGet("estado/{id:guid}")]
    public async Task<IActionResult> GetEstado(Guid id, CancellationToken ct = default)
    {
        var tx = await _store.GetAsync(id, ct);
        return tx is null
            ? NotFound(new { mensaje = "Transacción no encontrada" })
            : Ok(new { id = tx.Id, estado = tx.Estado });
    }

    // === Mapeo DTO ===
    private static AulaDto ToDTO(Aula a) => new()
    {
        Id = a.Id,
        Codigo = a.Codigo,
        Capacidad = a.Capacidad,
        Ubicacion = a.Ubicacion
    };
}