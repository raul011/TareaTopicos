using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos.request;
using TAREATOPICOS.ServicioA.Dtos.response;
using TAREATOPICOS.ServicioA.Services;
using TAREATOPICOS.ServicioA.Dtos;
using Microsoft.AspNetCore.Authorization;

namespace TAREATOPICOS.ServicioA.Controllers.Sincrono;

[ApiController]
[Route("api/[controller]")]
// [Authorize]
public class MateriasAsyncController : ControllerBase
{
    private readonly ServicioAContext _context;
    private readonly IBackgroundTaskQueue _queue;
    private readonly ITransaccionStore _store;

    public MateriasAsyncController(ServicioAContext context, IBackgroundTaskQueue queue, ITransaccionStore store)
    {
        _context = context;
        _queue = queue;
        _store = store;
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
    [HttpGet("{id:int}")]
    public async Task<ActionResult<MateriaRequestDto>> GetById(int id, CancellationToken ct = default)
    {
        var materia = await _context.Materias
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, ct);

        return materia is null ? NotFound() : Ok(ToDto(materia));
    }

    // POST: api/materias
    [HttpPost("async")]
    public async Task<IActionResult> CreateAsync([FromBody] MateriaRequestDto dto, CancellationToken ct = default)
    {
        if (await _context.Materias.AnyAsync(m => m.Codigo == dto.Codigo, ct))
            return Conflict(new { mensaje = $"El código de materia '{dto.Codigo}' ya existe." });

        var tx = new Transaccion
        {
            Entidad = "Materia",
            TipoOperacion = "CrearMateria",
            Payload = JsonSerializer.Serialize(dto),
            Estado = "EN_COLA"
        };

        await _store.AddAsync(tx);
        await _queue.EnqueueAsync(tx);

        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    // PUT: api/materias/{id}
    [HttpPut("async/{id:int}")]
    public async Task<IActionResult> UpdateAsync(int id, [FromBody] MateriaRequestDto dto, CancellationToken ct = default)
    {
        if (!await _context.Materias.AnyAsync(m => m.Id == id, ct))
            return NotFound(new { mensaje = "La materia no existe." });

        dto.Id = id;

        var tx = new Transaccion
        {
            Entidad = "Materia",
            TipoOperacion = "ActualizarMateria",
            Payload = JsonSerializer.Serialize(dto),
            Estado = "EN_COLA"
        };

        await _store.AddAsync(tx);
        await _queue.EnqueueAsync(tx);

        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    // DELETE: api/materias/{id}
    [HttpDelete("async/{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken ct = default)
    {
        if (!await _context.Materias.AnyAsync(m => m.Id == id, ct))
            return NotFound(new { mensaje = "La materia no existe." });

        var payload = new { Id = id };

        var tx = new Transaccion
        {
            Entidad = "Materia",
            TipoOperacion = "EliminarMateria",
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