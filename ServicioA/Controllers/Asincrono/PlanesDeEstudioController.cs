using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos.request;
using TAREATOPICOS.ServicioA.Dtos;
using TAREATOPICOS.ServicioA.Services;
using Microsoft.AspNetCore.Authorization;

namespace TAREATOPICOS.ServicioA.Controllers.Sincrono;

[ApiController]
[Route("api/[controller]")]
// [Authorize]
public class PlanesDeEstudioAsyncController : ControllerBase
{
    private readonly ServicioAContext _context;
    private readonly IBackgroundTaskQueue _queue;
    private readonly ITransaccionStore _store;

    public PlanesDeEstudioAsyncController(ServicioAContext context, IBackgroundTaskQueue queue, ITransaccionStore store)
    {
        _context = context;
        _queue = queue;
        _store = store;
    }
    
    // GET api/planesdeestudio
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PlanDeEstudioDto>>> Get(CancellationToken ct = default)
    {
        var items = await _context.PlanesEstudio
            .AsNoTracking()
            .OrderBy(p => p.Nombre)
            .ToListAsync(ct);

        return Ok(items.Select(ToDTO));
    }

    // GET api/planesdeestudio/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PlanDeEstudioDto>> GetById(int id, CancellationToken ct = default)
    {
        var plan = await _context.PlanesEstudio
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        return plan is null ? NotFound() : Ok(ToDTO(plan));
    }

    // POST api/planesdeestudio
    [HttpPost("async")]
    public async Task<IActionResult> CreateAsync([FromBody] PlanDeEstudioDto dto, CancellationToken ct = default)
    {
        if (await _context.PlanesEstudio.AnyAsync(p => p.Codigo == dto.Codigo, ct))
            return Conflict(new { mensaje = $"El código de plan '{dto.Codigo}' ya existe." });

        var tx = new Transaccion
        {
            Entidad = "PlanDeEstudio",
            TipoOperacion = "CrearPlan",
            Payload = JsonSerializer.Serialize(dto),
            Estado = "EN_COLA"
        };

        await _store.AddAsync(tx);
        await _queue.EnqueueAsync(tx);

        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    // PUT api/planesdeestudio/{id}
    [HttpPut("async/{id:int}")]
    public async Task<IActionResult> UpdateAsync(int id, [FromBody] PlanDeEstudioDto dto, CancellationToken ct = default)
    {
        if (!await _context.PlanesEstudio.AnyAsync(p => p.Id == id, ct))
            return NotFound(new { mensaje = "El plan de estudio no existe." });

        dto.Id = id;

        var tx = new Transaccion
        {
            Entidad = "PlanDeEstudio",
            TipoOperacion = "ActualizarPlan",
            Payload = JsonSerializer.Serialize(dto),
            Estado = "EN_COLA"
        };

        await _store.AddAsync(tx);
        await _queue.EnqueueAsync(tx);

        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }
    
    // GET api/planesdeestudio/{id}/materias
    [HttpGet("{id:int}/materias")]
    public async Task<ActionResult<IEnumerable<MateriaRequestDto>>> GetMateriasDePlan(int id, CancellationToken ct = default)
    {
    // 1. Validar existencia del plan
    var existePlan = await _context.PlanesEstudio
        .AsNoTracking()
        .AnyAsync(p => p.Id == id, ct);

    if (!existePlan)
        return NotFound($"No existe un PlanDeEstudio con Id={id}");

    // 2. Consultar materias asociadas al plan
    var materias = await _context.PlanMaterias
        .AsNoTracking()
        .Where(pm => pm.PlanId == id)
        .Include(pm => pm.Materia) // Incluimos datos de Materia
        .Select(pm => new MateriaRequestDto
        {
            Id = pm.Materia.Id,
            Codigo = pm.Materia.Codigo,
            Nombre = pm.Materia.Nombre,
            Creditos = pm.Materia.Creditos,
            NivelId = pm.Materia.NivelId
        })
        .OrderBy(m => m.Nombre)
        .ToListAsync(ct);

    // 3. Retornar resultado
    return Ok(materias);
}

    // DELETE api/planesdeestudio/{id}
    [HttpDelete("async/{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken ct = default)
    {
        if (!await _context.PlanesEstudio.AnyAsync(p => p.Id == id, ct))
            return NotFound(new { mensaje = "El plan de estudio no existe." });

        var payload = new { Id = id };

        var tx = new Transaccion
        {
            Entidad = "PlanDeEstudio",
            TipoOperacion = "EliminarPlan",
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

    private static PlanDeEstudioDto ToDTO(PlanDeEstudio p) => new()
    {
        Id = p.Id,
        Nombre = p.Nombre,
        Codigo = p.Codigo,
        Fecha = p.Fecha,
        Estado = p.Estado,
        CarreraId = p.CarreraId
    };
}