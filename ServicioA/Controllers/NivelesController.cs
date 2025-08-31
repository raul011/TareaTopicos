using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos;
using Microsoft.AspNetCore.Authorization;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NivelesController : ControllerBase
{
    private readonly ServicioAContext _context;
    public NivelesController(ServicioAContext context) => _context = context;

    // GET: api/niveles
    [HttpGet]
    public async Task<ActionResult<IEnumerable<NivelDto>>> GetAll(CancellationToken ct)
    {
        var list = await _context.Niveles
            .AsNoTracking()
            .OrderBy(n => n.Numero)
            .ToListAsync(ct);

        return Ok(list.Select(ToDto));
    }

    // GET: api/niveles/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<NivelDto>> Get(int id, CancellationToken ct)
    {
        var nivel = await _context.Niveles
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == id, ct);

        return nivel is null ? NotFound() : Ok(ToDto(nivel));
    }

    // POST: api/niveles
    [HttpPost]
    public async Task<ActionResult<NivelDto>> Create([FromBody] NivelDto dto, CancellationToken ct)
    {
        var nivel = new Nivel
        {
            Numero = dto.Numero,
            Nombre = dto.Nombre
        };

        _context.Niveles.Add(nivel);
        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = nivel.Id }, ToDto(nivel));
    }

    // PUT: api/niveles/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] NivelDto dto, CancellationToken ct)
    {
        var nivel = await _context.Niveles.FirstOrDefaultAsync(n => n.Id == id, ct);
        if (nivel is null) return NotFound();

        nivel.Numero = dto.Numero;
        nivel.Nombre = dto.Nombre;

        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    // DELETE: api/niveles/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var nivel = await _context.Niveles.FirstOrDefaultAsync(n => n.Id == id, ct);
        if (nivel is null) return NotFound();

        _context.Niveles.Remove(nivel);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    // Mapper
    private static NivelDto ToDto(Nivel n) => new()
    {
        Id = n.Id,
        Numero = n.Numero,
        Nombre = n.Nombre
    };



}