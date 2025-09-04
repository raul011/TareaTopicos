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
    private readonly RedisTransaccionStore _store;


    public AulasController(ServicioAContext context, IBackgroundTaskQueue queue, RedisTransaccionStore store)

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

    // POST asincrónico: api/aulas/async
    [HttpPost("async")]
    public async Task<IActionResult> CreateAsync([FromBody] AulaDto dto)
    {
        var transaccion = new Transaccion
        {
            TipoOperacion = "POST",
            Entidad = "Aula",
            Payload = JsonSerializer.Serialize(dto)
        };

        await _store.AddAsync(transaccion);
        _queue.Enqueue(transaccion);

        return AcceptedAtAction("GetEstado", "Transacciones", new { id = transaccion.Id }, new { id = transaccion.Id, estado = transaccion.Estado });
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

    // PUT asincrónico: api/aulas/async/{id}
    [HttpPut("async/{id:int}")]
    public async Task<IActionResult> UpdateAsync(int id, [FromBody] AulaDto dto)
    {
        dto.Id = id; // Aseguramos que el ID del DTO coincida con el de la URL
        var transaccion = new Transaccion
        {
            TipoOperacion = "PUT",
            Entidad = "Aula",
            Payload = JsonSerializer.Serialize(dto)
        };

        await _store.AddAsync(transaccion);
        _queue.Enqueue(transaccion);

        return AcceptedAtAction("GetEstado", "Transacciones", new { id = transaccion.Id }, new { id = transaccion.Id, estado = transaccion.Estado });
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

    // DELETE asincrónico: api/aulas/async/{id}
    [HttpDelete("async/{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var transaccion = new Transaccion
        {
            TipoOperacion = "DELETE",
            Entidad = "Aula",
            Payload = JsonSerializer.Serialize(new { Id = id })
        };

        await _store.AddAsync(transaccion);
        _queue.Enqueue(transaccion);

        return AcceptedAtAction("GetEstado", "Transacciones", new { id = transaccion.Id }, new { id = transaccion.Id, estado = transaccion.Estado });
    }

    private static AulaDto ToDTO(Aula a) => new()
    {
        Id = a.Id,
        Codigo = a.Codigo,
        Capacidad = a.Capacidad,
        Ubicacion = a.Ubicacion
    };
}