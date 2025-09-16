using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos;
using TAREATOPICOS.ServicioA.Services;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AulasController : ControllerBase
{
    private readonly ServicioAContext _context;
    private readonly IBackgroundTaskQueue _queue;
    private readonly ITransaccionStore _store;

    public AulasController(ServicioAContext context, IBackgroundTaskQueue queue, ITransaccionStore store)
    {
        _context = context;
        _queue = queue;
        _store = store;
    }

    #region Endpoints Síncronos
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
    [HttpPost]
    public async Task<ActionResult<AulaDto>> Create([FromBody] AulaDto dto, CancellationToken ct = default)
    {
        // Validación: Asegurar que el código del aula sea único.
        if (await _context.Aulas.AnyAsync(a => a.Codigo == dto.Codigo, ct))
            return Conflict($"Ya existe un aula con el código '{dto.Codigo}'.");

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

    // PUT api/aulas/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] AulaDto dto, CancellationToken ct = default)
    {
        var aula = await _context.Aulas.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (aula is null) return NotFound();

        aula.Codigo = dto.Codigo;
        aula.Capacidad = dto.Capacidad;
        aula.Ubicacion = dto.Ubicacion;

        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    // DELETE api/aulas/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        var aula = await _context.Aulas.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (aula is null) return NotFound();

        _context.Aulas.Remove(aula);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }
    #endregion

    #region Endpoints Asíncronos
    // POST: api/aulas/async
    [HttpPost("async")]
    public async Task<IActionResult> CreateAsync([FromBody] AulaDto dto, CancellationToken ct)
    {
        dto.Id = 0;
        return await EnqueueTransaction("CREATE", dto, ct);
    }

    // PUT: api/aulas/async/{id}
    [HttpPut("async/{id:int}")]
    public async Task<IActionResult> UpdateAsync(int id, [FromBody] AulaDto dto, CancellationToken ct)
    {
        dto.Id = id;
        return await EnqueueTransaction("UPDATE", dto, ct);
    }

    // DELETE: api/aulas/async/{id}
    [HttpDelete("async/{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken ct)
    {
        var dto = new AulaDto { Id = id };
        return await EnqueueTransaction("DELETE", dto, ct);
    }
    #endregion

    #region Métodos Privados
    private async Task<IActionResult> EnqueueTransaction(string operation, object payload, CancellationToken ct)
    {
        var tx = new Transaccion
        {
            Id = Guid.NewGuid(),
            Entidad = "Aula",
            TipoOperacion = operation,
            Payload = JsonSerializer.Serialize(payload),
            Estado = "EN_COLA",
            NotBefore = DateTimeOffset.UtcNow
        };

        await _store.AddAsync(tx, ct);
        await _queue.EnqueueAsync(tx, "default", ct);

        return AcceptedAtAction(nameof(TransaccionesController.Get), "Transacciones", new { id = tx.Id }, new { transaccionId = tx.Id, estado = tx.Estado });
    }

    #endregion

    #region Mappers
    private static AulaDto ToDTO(Aula a) => new()
    {
        Id = a.Id,
        Codigo = a.Codigo,
        Capacidad = a.Capacidad,
        Ubicacion = a.Ubicacion
    };
    #endregion
}