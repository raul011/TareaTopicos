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
// [Authorize]
[AllowAnonymous] 
public class MateriasController : ControllerBase
{
    private readonly ServicioAContext _db;
    private readonly QueueManager _qm;
    private readonly ITransaccionStore _store;
    private readonly IConfiguration _cfg;
    private readonly ILogger<MateriasController> _logger;
    private readonly WorkerHost _workerHost;
    public MateriasController(
        ServicioAContext db,
        QueueManager qm,
        ITransaccionStore store,
        IConfiguration cfg,
        ILogger<MateriasController> logger, 
        WorkerHost workerHost)
    {
        _db = db;
        _qm = qm;
        _store = store;
        _cfg = cfg;
        _logger = logger;
        _workerHost = workerHost;
    }

    // ===  ENDPOINTS SÍNCRONOS (LECTURA Y ESCRITURA DIRECTA) ===
   
    [HttpGet]
    public async Task<ActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 5,
        CancellationToken ct = default)
    {
        _logger.LogInformation("📥 GET /api/materias?page={Page}&pageSize={PageSize}", page, pageSize);

        if (page <= 0 || pageSize <= 0)
        {
            _logger.LogWarning("⚠️ Parámetros inválidos de paginación: page={Page}, pageSize={PageSize}", page, pageSize);
            return BadRequest("Los parámetros de paginación deben ser mayores a cero.");
        }

        var query = _db.Materias
            .Include(m => m.Nivel)
            .AsNoTracking()
            .OrderBy(m => m.Codigo);

        var totalItems = await query.CountAsync(ct);
        var materias = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        _logger.LogInformation("✅ Devueltas {Count} materias de un total de {Total}", materias.Count, totalItems);

        return Ok(new
        {
            TotalItems = totalItems,
            Page = page,
            PageSize = pageSize,
            Items = materias.Select(ToResponseDto)
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MateriaRequestDto>> GetById(int id, CancellationToken ct = default)
    {
        _logger.LogInformation("📥 GET /api/materias/{Id}", id);

        var materia = await _db.Materias.Include(m => m.Nivel).AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, ct);

        if (materia is null)
        {
            _logger.LogWarning("⚠️ Materia {Id} no encontrada", id);
            return NotFound();
        }

        _logger.LogInformation("✅ Materia {Id} encontrada", id);
        return Ok(ToResponseDto(materia));
    }



[HttpPost]
public async Task<ActionResult<MateriaRequestDto>> Create([FromBody] MateriaRequestDto dto, CancellationToken ct = default)
{
    _logger.LogInformation("📥 POST /api/materias → creando/obteniendo {Codigo}", dto.Codigo);

    // Validaciones básicas
    if (string.IsNullOrWhiteSpace(dto.Codigo))
        return BadRequest(new { mensaje = "Codigo es requerido" });
    if (string.IsNullOrWhiteSpace(dto.Nombre))
        return BadRequest(new { mensaje = "Nombre es requerido" });
    if (dto.Creditos <= 0)
        return BadRequest(new { mensaje = "Creditos debe ser > 0" });
    if (dto.NivelId <= 0)
        return BadRequest(new { mensaje = "NivelId debe ser > 0" });

    // Verificar nivel
    var nivelExiste = await _db.Niveles.AsNoTracking().AnyAsync(n => n.Id == dto.NivelId, ct);
    if (!nivelExiste)
        return BadRequest(new { mensaje = $"NivelId {dto.NivelId} no existe" });

    // Idempotencia: si ya existe por código → 200 OK con la existente
    var existente = await _db.Materias.AsNoTracking()
        .FirstOrDefaultAsync(m => m.Codigo == dto.Codigo, ct);
    if (existente is not null)
    {
        _logger.LogInformation("ℹ️ POST idempotente: {Codigo} ya existía (OK 200)", dto.Codigo);
        return Ok(ToDto(existente));
    }

    var entity = new Materia
    {
        Codigo   = dto.Codigo,
        Nombre   = dto.Nombre,
        Creditos = dto.Creditos,
        NivelId  = dto.NivelId
    };

    try
    {
        _db.Materias.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("✅ Creada {Codigo} con Id {Id}", entity.Codigo, entity.Id);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ToDto(entity));
    }
    catch (DbUpdateException ex) when ((ex.InnerException?.Message ?? ex.Message).ToLowerInvariant().Contains("unique"))
    {
        // Carrera: alguien insertó la misma materia justo ahora → devolver 200 con la existente
        var ya = await _db.Materias.AsNoTracking().FirstOrDefaultAsync(m => m.Codigo == dto.Codigo, ct);
        if (ya is not null)
        {
            _logger.LogInformation("ℹ️ POST idempotente tras carrera: {Codigo} (OK 200)", dto.Codigo);
            return Ok(ToDto(ya));
        }
        throw; // si no la encontramos, re-lanzar para ver el error real
    }
}







 [HttpPut("codigo/{codigo}")]
public async Task<IActionResult> UpdateByCodigo(
    string codigo,
    [FromBody] MateriaRequestDto dto,
    CancellationToken ct = default)
{
    _logger.LogInformation("📥 PUT /api/materias/codigo/{Codigo}", codigo);

    // Buscar la materia por código (ruta)
    var materia = await _db.Materias.FirstOrDefaultAsync(m => m.Codigo == codigo, ct);
    if (materia is null)
    {
        _logger.LogWarning("⚠️ Materia con código {Codigo} no encontrada", codigo);
        return NotFound(new { mensaje = $"Materia código {codigo} no encontrada" });
    }

    // Validaciones básicas
    if (string.IsNullOrWhiteSpace(dto.Nombre))
        return BadRequest(new { mensaje = "Nombre es requerido" });
    if (dto.Creditos <= 0)
        return BadRequest(new { mensaje = "Creditos debe ser > 0" });
    if (dto.NivelId <= 0)
        return BadRequest(new { mensaje = "NivelId debe ser > 0" });

    // Verificar que el nivel exista
    var nivelExiste = await _db.Niveles.AsNoTracking().AnyAsync(n => n.Id == dto.NivelId, ct);
    if (!nivelExiste)
        return BadRequest(new { mensaje = $"NivelId {dto.NivelId} no existe" });

    // Evitar cambio de código por body (si viene, debe coincidir)
    if (!string.IsNullOrWhiteSpace(dto.Codigo) && !string.Equals(dto.Codigo, codigo, StringComparison.OrdinalIgnoreCase))
        return BadRequest(new { mensaje = "No se permite cambiar el código de la materia" });

    // Idempotencia: si no hay cambios reales, no tocar DB
    if (materia.Nombre == dto.Nombre &&
        materia.Creditos == dto.Creditos &&
        materia.NivelId == dto.NivelId)
    {
        _logger.LogInformation("ℹ️ PUT idempotente: {Codigo} ya tenía los mismos datos", codigo);
        return NoContent(); // mantiene el 204
    }

    // Aplicar cambios
    materia.Nombre   = dto.Nombre;
    materia.Creditos = dto.Creditos;
    materia.NivelId  = dto.NivelId;

    await _db.SaveChangesAsync(ct);

    _logger.LogInformation("✅ Materia {Codigo} actualizada correctamente", codigo);
    return NoContent(); // 204 para el test
}


 [HttpDelete("codigo/{codigo}")]
public async Task<IActionResult> DeleteByCodigo(string codigo, CancellationToken ct = default)
{
    _logger.LogInformation("📥 DELETE /api/materias/codigo/{Codigo}", codigo);

    var materia = await _db.Materias.FirstOrDefaultAsync(m => m.Codigo == codigo, ct);
    if (materia is null)
    {
        _logger.LogWarning("⚠️ Materia con código {Codigo} no encontrada para eliminar", codigo);
        return NotFound(new { mensaje = $"Materia código {codigo} no encontrada" });
    }

    try
    {
        _db.Materias.Remove(materia);
        await _db.SaveChangesAsync(ct);
    }
    catch (DbUpdateException ex) when ((ex.InnerException?.Message ?? ex.Message)
                .Contains("foreign key", StringComparison.OrdinalIgnoreCase))
    {
        _logger.LogError(ex, "💥 Error de FK al eliminar materia {Codigo}", codigo);
        return Conflict(new { mensaje = $"No se puede eliminar la materia {codigo} porque tiene dependencias (FK)." });
    }

    _logger.LogInformation("✅ Materia {Codigo} eliminada correctamente", codigo);
    return NoContent(); // 204
}











































































    

    // === ENDPOINTS ASÍNCRONOS ===

    [HttpPost("async")]
    public async Task<IActionResult> CrearMateriaAsync(
        [FromBody] MateriaRequestDto materiaDto,
        [FromQuery] string? queue = "default",
        [FromQuery] int priority = 1,
        [FromQuery] DateTimeOffset? notBeforeUtc = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("📥 POST /api/materias/async → encolando creación {Codigo}", materiaDto.Codigo);

        var existe = await _db.Materias.AnyAsync(m => m.Codigo == materiaDto.Codigo, ct);
        if (existe)
        {
            _logger.LogWarning("⚠️ Materia con código {Codigo} ya existe", materiaDto.Codigo);
            return Conflict(new { mensaje = $"Ya existe una materia con el código '{materiaDto.Codigo}'." });
        }

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

        _logger.LogInformation("✅ Tx {TxId} encolada (POST Materia)", tx.Id);

        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    [HttpPut("async/codigo/{codigo}")]
    public async Task<IActionResult> ActualizarMateriaAsyncPorCodigo(
        string codigo,
        [FromBody] MateriaRequestDto dto,
        [FromQuery] string? queue = "default",
        [FromQuery] int priority = 1,
        [FromQuery] DateTimeOffset? notBeforeUtc = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("📥 PUT /api/materias/async/codigo/{Codigo} → encolando actualización", codigo);

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

        _logger.LogInformation("✅ Tx {TxId} encolada (PUT Materia)", tx.Id);

        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    [HttpDelete("async/codigo/{codigo}")]
    public async Task<IActionResult> EliminarMateriaAsyncPorCodigo(
        string codigo,
        [FromQuery] string? queue = "default",
        [FromQuery] int priority = 1,
        [FromQuery] DateTimeOffset? notBeforeUtc = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("📥 DELETE /api/materias/async/codigo/{Codigo} → encolando eliminación", codigo);

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

        _logger.LogInformation("✅ Tx {TxId} encolada (DELETE Materia)", tx.Id);

        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    [HttpGet("estado/{id:guid}")]
    public async Task<IActionResult> GetEstado(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("📥 GET /api/materias/estado/{Id}", id);

        var tx = await _store.GetAsync(id, ct);
        if (tx is null)
        {
            _logger.LogWarning("⚠️ Transacción {Id} no encontrada", id);
            return NotFound(new { mensaje = "Transacción no encontrada" });
        }

        _logger.LogInformation("✅ Estado encontrado para Tx {Id}: {Estado}", tx.Id, tx.Estado);

        return Ok(new { id = tx.Id, estado = tx.Estado });
    }

    // === Mapeo interno ===
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
