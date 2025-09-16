using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos.request;
using TAREATOPICOS.ServicioA.Dtos.response;
using TAREATOPICOS.ServicioA.Dtos;
using Microsoft.AspNetCore.Authorization;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MateriasController : ControllerBase
{
    private readonly ServicioAContext _context;

    public MateriasController(ServicioAContext context)
    {
        _context = context;
    }
    /*
    // GET: api/materias
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MateriaResponseDto>>> GetAll(CancellationToken ct = default)
    {
        var materias = await _context.Materias
            .Include(m => m.Nivel) // Asegura que se cargue el Nivel asociado
            .AsNoTracking()
            .OrderBy(m => m.Codigo)
            .ToListAsync(ct);

        return Ok(materias.Select(ToResponseDto));
    }

   */

    // GET: api/materias?page=1&pageSize=10
    [HttpGet]
    public async Task<ActionResult> GetAll(
     [FromQuery] int page = 1,
     [FromQuery] int pageSize = 5,
     CancellationToken ct = default)
    {
        if (page <= 0 || pageSize <= 0)
            return BadRequest("Los parámetros de paginación deben ser mayores a cero.");

        var query = _context.Materias
            .Include(m => m.Nivel)
            .AsNoTracking()
            .OrderBy(m => m.Codigo);

        var totalItems = await query.CountAsync(ct);
        var materias = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var result = new
        {
            TotalItems = totalItems,
            Page = page,
            PageSize = pageSize,
            Items = materias.Select(ToResponseDto)
        };

        return Ok(result);
    }

    // GET: api/materias/{id}
    [HttpGet("{codigo}")]
    public async Task<ActionResult<MateriaRequestDto>> GetByCodigo(string codigo, CancellationToken ct = default)
    {
        var materia = await _context.Materias
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Codigo == codigo, ct);

        return materia is null ? NotFound() : Ok(ToDto(materia));
    }

    // POST: api/materias
    [HttpPost]
    public async Task<ActionResult<MateriaRequestDto>> Create([FromBody] MateriaRequestDto dto, CancellationToken ct = default)
    {
        // Validación: Asegurar que el código de la materia sea único.
        if (await _context.Materias.AnyAsync(m => m.Codigo == dto.Codigo, ct))
            return Conflict($"Ya existe una materia con el código '{dto.Codigo}'.");

        var entity = new Materia
        {
            Codigo = dto.Codigo,
            Nombre = dto.Nombre,
            Creditos = dto.Creditos,
            NivelId = dto.NivelId
        };

        _context.Materias.Add(entity);
        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetByCodigo), new { codigo = entity.Codigo }, ToDto(entity));
    }

    // PUT: api/materias/{id}
    [HttpPut("{codigo}")]
    public async Task<IActionResult> Update(string codigo, [FromBody] MateriaRequestDto dto, CancellationToken ct = default)
    {
        var materia = await _context.Materias.FirstOrDefaultAsync(m => m.Codigo == codigo, ct);
        if (materia is null) return NotFound();

        materia.Nombre = dto.Nombre;
        materia.Creditos = dto.Creditos;
        materia.NivelId = dto.NivelId;
        // El código de la materia no debería cambiar, ya que es su identificador natural.

        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    // DELETE: api/materias/{id}
    [HttpDelete("{codigo}")]
    public async Task<IActionResult> Delete(string codigo, CancellationToken ct = default)
    {
        var materia = await _context.Materias.FirstOrDefaultAsync(m => m.Codigo == codigo, ct);
        if (materia is null) return NotFound();

        _context.Materias.Remove(materia);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    // Mapeo interno
    private static MateriaRequestDto ToDto(Materia m) => new()
    {
        Id = m.Id,
        Codigo = m.Codigo,
        Nombre = m.Nombre,
        Creditos = m.Creditos,
        NivelId = m.NivelId
    };

    private static MateriaResponseDto ToResponseDto(Materia m) => new()
    {
        Id = m.Id,
        Codigo = m.Codigo,
        Nombre = m.Nombre,
        Creditos = m.Creditos,
        Nivel = new NivelDto
        {
            Id = m.Nivel.Id,
            Numero = m.Nivel.Numero,
            Nombre = m.Nivel.Nombre
        }
    };
}