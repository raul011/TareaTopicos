using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos;
using Microsoft.AspNetCore.Authorization;
using TAREATOPICOS.ServicioA.Services;
using System.Text.Json;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocentesController : ControllerBase
{
    private readonly ServicioAContext _context;
    private readonly IBackgroundTaskQueue _queue;
    private readonly RedisTransaccionStore _store;

    public DocentesController(ServicioAContext context, IBackgroundTaskQueue queue, RedisTransaccionStore store)
    {
        _context = context;
        _queue = queue;
        _store = store;
    }

    // GET: api/docentes
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DocenteDto>>> GetAll(CancellationToken ct = default)
    {
        var docentes = await _context.Docentes
            .AsNoTracking()
            .OrderBy(d => d.Nombre)
            .ToListAsync(ct);

        return Ok(docentes.Select(ToDto));
    }

    // GET: api/docentes/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<DocenteDto>> GetById(int id, CancellationToken ct = default)
    {
        var docente = await _context.Docentes
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        return docente is null ? NotFound() : Ok(ToDto(docente));
    }

    // POST: api/docentes
    [HttpPost]
    public async Task<ActionResult<DocenteDto>> Create([FromBody] DocenteDto dto, CancellationToken ct = default)
    {
        var entity = new Docente
        {
            Registro = dto.Registro,
            Ci = dto.Ci,
            Nombre = dto.Nombre,
            Telefono = dto.Telefono,
            Estado = string.IsNullOrWhiteSpace(dto.Estado) ? "ACTIVO" : dto.Estado
        };

        _context.Docentes.Add(entity);
        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ToDto(entity));
    }

    // POST asincrónico: api/docentes/async
    [HttpPost("async")]
    public async Task<IActionResult> CreateAsync([FromBody] DocenteDto dto)
    {
        var transaccion = new Transaccion
        {
            TipoOperacion = "POST",
            Entidad = "Docente",
            Payload = JsonSerializer.Serialize(dto)
        };

        await _store.AddAsync(transaccion);
        _queue.Enqueue(transaccion);

        // Devolvemos el ID de la transacción para futura consulta de estado
        return AcceptedAtAction("GetEstado", "Transacciones", new { id = transaccion.Id }, new { id = transaccion.Id, estado = transaccion.Estado });
    }

    // PUT: api/docentes/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] DocenteDto dto, CancellationToken ct = default)
    {
        var docente = await _context.Docentes.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (docente is null) return NotFound();

        docente.Registro = dto.Registro;
        docente.Ci = dto.Ci;
        docente.Nombre = dto.Nombre;
        docente.Telefono = dto.Telefono;
        docente.Estado = string.IsNullOrWhiteSpace(dto.Estado) ? docente.Estado : dto.Estado;

        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    // PUT asincrónico: api/docentes/async/{id}
    [HttpPut("async/{id:int}")]
    public async Task<IActionResult> UpdateAsync(int id, [FromBody] DocenteDto dto)
    {
        dto.Id = id; // Aseguramos que el ID del DTO coincida con el de la URL
        var transaccion = new Transaccion
        {
            TipoOperacion = "PUT",
            Entidad = "Docente",
            Payload = JsonSerializer.Serialize(dto)
        };

        await _store.AddAsync(transaccion);
        _queue.Enqueue(transaccion);

        return AcceptedAtAction("GetEstado", "Transacciones", new { id = transaccion.Id }, new { id = transaccion.Id, estado = transaccion.Estado });
    }

    // DELETE: api/docentes/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        var docente = await _context.Docentes.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (docente is null) return NotFound();

        _context.Docentes.Remove(docente);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    // DELETE asincrónico: api/docentes/async/{id}
    [HttpDelete("async/{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var transaccion = new Transaccion
        {
            TipoOperacion = "DELETE",
            Entidad = "Docente",
            Payload = JsonSerializer.Serialize(new { Id = id })
        };

        await _store.AddAsync(transaccion);
        _queue.Enqueue(transaccion);

        return AcceptedAtAction("GetEstado", "Transacciones", new { id = transaccion.Id }, new { id = transaccion.Id, estado = transaccion.Estado });
    }

    private static DocenteDto ToDto(Docente d) => new()
    {
        Id = d.Id,
        Registro = d.Registro,
        Ci = d.Ci,
        Nombre = d.Nombre,
        Telefono = d.Telefono,
        Estado = d.Estado
    };
}