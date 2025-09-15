using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos;
using TAREATOPICOS.ServicioA.Services;
using Microsoft.AspNetCore.Authorization;

namespace TAREATOPICOS.ServicioA.Controllers.Sincrono;

[ApiController]
[Route("api/[controller]")]
// [Authorize]
public class AulasAsyncController : ControllerBase
{
    private readonly ServicioAContext _context;
    private readonly IBackgroundTaskQueue _queue;
    private readonly ITransaccionStore _store;

    public AulasAsyncController(ServicioAContext context, IBackgroundTaskQueue queue, ITransaccionStore store)
    {
        _context = context;
        _queue = queue;
        _store = store;
    }

    // GET api/aulas
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AulaDto>>> Get(CancellationToken ct = default)
    {
        var items = await _context.Aulas
            .AsNoTracking()
            .OrderBy(a => a.Codigo)
            .ToListAsync(ct);

        return Ok(items.Select(ToDTO));
    }

    // GET api/aulas/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AulaDto>> GetById(int id, CancellationToken ct = default)
    {
        var aula = await _context.Aulas
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return aula is null ? NotFound() : Ok(ToDTO(aula));
    }

    // POST api/aulas
    [HttpPost("async")]
    public async Task<IActionResult> CreateAsync([FromBody] AulaDto dto, CancellationToken ct = default)
    {
        if (await _context.Aulas.AnyAsync(a => a.Codigo == dto.Codigo, ct))
            return Conflict(new { mensaje = $"El código de aula '{dto.Codigo}' ya existe." });

        var tx = new Transaccion
        {
            Entidad = "Aula",
            TipoOperacion = "CrearAula",
            Payload = JsonSerializer.Serialize(dto),
            Estado = "EN_COLA"
        };

        await _store.AddAsync(tx);
        await _queue.EnqueueAsync(tx);

        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    // PUT api/aulas/{id}
    [HttpPut("async/{id:int}")]
    public async Task<IActionResult> UpdateAsync(int id, [FromBody] AulaDto dto, CancellationToken ct = default)
    {
        if (!await _context.Aulas.AnyAsync(a => a.Id == id, ct))
            return NotFound(new { mensaje = "El aula no existe." });

        dto.Id = id; // Aseguramos que el ID esté en el payload

        var tx = new Transaccion
        {
            Entidad = "Aula",
            TipoOperacion = "ActualizarAula",
            Payload = JsonSerializer.Serialize(dto),
            Estado = "EN_COLA"
        };

        await _store.AddAsync(tx);
        await _queue.EnqueueAsync(tx);

        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    // DELETE api/aulas/{id}
    [HttpDelete("async/{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken ct = default)
    {
        if (!await _context.Aulas.AnyAsync(a => a.Id == id, ct))
            return NotFound(new { mensaje = "El aula no existe." });

        var payload = new { Id = id };

        var tx = new Transaccion
        {
            Entidad = "Aula",
            TipoOperacion = "EliminarAula",
            Payload = JsonSerializer.Serialize(payload),
            Estado = "EN_COLA"
        };

        await _store.AddAsync(tx);
        await _queue.EnqueueAsync(tx);

        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    [HttpGet("estado/{txId:guid}")]
    public async Task<IActionResult> Estado(Guid txId, CancellationToken ct)
    {
        var tx = await _store.GetAsync(txId);
        if (tx is null) return NotFound(new { mensaje = "Transacción no encontrada" });
        return Ok(new { id = tx.Id, estado = tx.Estado });
    }

    private static AulaDto ToDTO(Aula a) => new()
    {
        Id = a.Id,
        Codigo = a.Codigo,
        Capacidad = a.Capacidad,
        Ubicacion = a.Ubicacion
    };
}