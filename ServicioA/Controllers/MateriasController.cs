using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos.request;
using TAREATOPICOS.ServicioA.Dtos.response;
using TAREATOPICOS.ServicioA.Dtos;
using TAREATOPICOS.ServicioA.Services;
using Microsoft.AspNetCore.Authorization;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MateriasController : ControllerBase
{
    private readonly ServicioAContext _db;
    private readonly QueueManager _qm;
    private readonly ITransaccionStore _store;
    private readonly IConfiguration _cfg;

    public MateriasController(ServicioAContext db, QueueManager qm, ITransaccionStore store, IConfiguration cfg)
    {
        _db = db;
        _qm = qm;
        _store = store;
        _cfg = cfg;
    }

    // ===  ENDPOINTS SÍNCRONOS (LECTURA Y ESCRITURA DIRECTA) ===
    /*
    // GET: api/materias
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MateriaResponseDto>>> GetAll(CancellationToken ct = default)
    {
        var materias = await _db.Materias
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

        var query = _db.Materias
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
        var materia = await _db.Materias
            .Include(m => m.Nivel)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, ct);

        return materia is null ? NotFound() : Ok(ToResponseDto(materia));
    }

    // POST: api/materias
    [HttpPost]
public async Task<ActionResult<MateriaRequestDto>> Create([FromBody] MateriaRequestDto dto, CancellationToken ct = default)
{
    var existe = await _db.Materias.AnyAsync(m => m.Codigo == dto.Codigo, ct);
    if (existe)
        return Conflict(new { mensaje = $"Ya existe una materia con el código '{dto.Codigo}'." });

    var entity = new Materia
    {
        Codigo = dto.Codigo,
        Nombre = dto.Nombre,
        Creditos = dto.Creditos,
        NivelId = dto.NivelId
    };

    _db.Materias.Add(entity);
    await _db.SaveChangesAsync(ct);

    return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ToDto(entity));
}

    // PUT: api/materias/{id}
    [HttpPut("codigo/{codigo}")]
public async Task<IActionResult> UpdateByCodigo(string codigo, [FromBody] MateriaRequestDto dto, CancellationToken ct = default)
{
    var materia = await _db.Materias.FirstOrDefaultAsync(m => m.Codigo == codigo, ct);
    if (materia is null) return NotFound();

    materia.Nombre = dto.Nombre;
    materia.Creditos = dto.Creditos;
    materia.NivelId = dto.NivelId;

    await _db.SaveChangesAsync(ct);
    return NoContent();
}

    // DELETE: api/materias/{id}
    [HttpDelete("codigo/{codigo}")]
public async Task<IActionResult> DeleteByCodigo(string codigo, CancellationToken ct = default)
{
    var materia = await _db.Materias.FirstOrDefaultAsync(m => m.Codigo == codigo, ct);
    if (materia is null) return NotFound();

    _db.Materias.Remove(materia);
    await _db.SaveChangesAsync(ct);
    return NoContent();
}

    // ===  ENDPOINTS ASÍNCRONOS (ESCRITURA ENCOLADA) ===

    // POST /api/materias/async
    [HttpPost("async")]
public async Task<IActionResult> CrearMateriaAsync(
    [FromBody] MateriaRequestDto materiaDto,
    [FromQuery] string? queue = "default",
    [FromQuery] int priority = 1,
    [FromQuery] DateTimeOffset? notBeforeUtc = null,
    CancellationToken ct = default)
{
    var existe = await _db.Materias.AnyAsync(m => m.Codigo == materiaDto.Codigo, ct);
    if (existe)
        return Conflict(new { mensaje = $"Ya existe una materia con el código '{materiaDto.Codigo}'." });

    var tx = new Transaccion
    {
        TipoOperacion = "POST",
        Entidad = "Materia",
        Payload = JsonSerializer.Serialize(materiaDto),
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

    // PUT /api/materias/async/{id}
    [HttpPut("async/codigo/{codigo}")]
public async Task<IActionResult> ActualizarMateriaAsyncPorCodigo(
    string codigo,
    [FromBody] MateriaRequestDto dto,
    [FromQuery] string? queue = "default",
    [FromQuery] int priority = 1,
    [FromQuery] DateTimeOffset? notBeforeUtc = null,
    CancellationToken ct = default)
{
    dto.Codigo = codigo;

    var tx = new Transaccion
    {
        TipoOperacion = "PUT",
        Entidad = "Materia",
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

    // DELETE /api/materias/async/{id}
    [HttpDelete("async/codigo/{codigo}")]
public async Task<IActionResult> EliminarMateriaAsyncPorCodigo(
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
        Entidad = "Materia",
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

    // GET /api/materias/estado/{id}
    [HttpGet("estado/{id:guid}")]
    public async Task<IActionResult> GetEstado(Guid id, CancellationToken ct = default)
    {
        var tx = await _store.GetAsync(id, ct);
        return tx is null
            ? NotFound(new { mensaje = "Transacción no encontrada" })
            : Ok(new { id = tx.Id, estado = tx.Estado });
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
        { // El Nivel debe estar cargado (Include) para que esto no falle
            Id = m.Nivel.Id,
            Numero = m.Nivel.Numero,
            Nombre = m.Nivel.Nombre
        }
    };
}