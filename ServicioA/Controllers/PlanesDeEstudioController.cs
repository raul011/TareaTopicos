using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Asegurarse que este using esté presente
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos.request;
using TAREATOPICOS.ServicioA.Dtos;
using Microsoft.AspNetCore.Authorization;
using TAREATOPICOS.ServicioA.Services;
using System.Text.Json;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PlanesDeEstudioController : ControllerBase
{
    private readonly ServicioAContext _context;
    private readonly IBackgroundTaskQueue _queue;
    private readonly ITransaccionStore _store;
    public PlanesDeEstudioController(ServicioAContext context, IBackgroundTaskQueue queue, ITransaccionStore store)
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
    [HttpPost]
    public async Task<ActionResult<PlanDeEstudioDto>> Create([FromBody] PlanDeEstudioDto dto, CancellationToken ct = default)
    {
        // Validación: Asegurar que el código del plan de estudio sea único.
        if (await _context.PlanesEstudio.AnyAsync(p => p.Codigo == dto.Codigo, ct))
            return Conflict($"Ya existe un plan de estudio con el código '{dto.Codigo}'.");

        var entity = new PlanDeEstudio
        {
            Nombre = dto.Nombre,
            Codigo = dto.Codigo,
            Fecha = dto.Fecha,
            Estado = string.IsNullOrWhiteSpace(dto.Estado) ? "ACTIVO" : dto.Estado,
            CarreraId = dto.CarreraId
        };

        _context.PlanesEstudio.Add(entity);
        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ToDTO(entity));
    }

    // PUT api/planesdeestudio/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] PlanDeEstudioDto dto, CancellationToken ct = default)
    {
        var plan = await _context.PlanesEstudio.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (plan is null) return NotFound();

        plan.Nombre = dto.Nombre;
        plan.Codigo = dto.Codigo;
        plan.Fecha = dto.Fecha;
        plan.Estado = dto.Estado;
        plan.CarreraId = dto.CarreraId;

        await _context.SaveChangesAsync(ct);
        return NoContent();
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

    #region Endpoints Asíncronos
    // POST: api/planesdeestudio/async
    [HttpPost("async")]
    public async Task<IActionResult> CreateAsync([FromBody] PlanDeEstudioDto dto, CancellationToken ct)
    {
        dto.Id = 0;
        return await EnqueueTransaction("CREATE", dto, ct);
    }

    // PUT: api/planesdeestudio/async/{id}
    [HttpPut("async/{id:int}")]
    public async Task<IActionResult> UpdateAsync(int id, [FromBody] PlanDeEstudioDto dto, CancellationToken ct)
    {
        dto.Id = id;
        return await EnqueueTransaction("UPDATE", dto, ct);
    }

    // DELETE: api/planesdeestudio/async/{id}
    [HttpDelete("async/{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken ct)
    {
        var dto = new PlanDeEstudioDto { Id = id };
        return await EnqueueTransaction("DELETE", dto, ct);
    }
    #endregion

    #region Métodos Privados
    private async Task<IActionResult> EnqueueTransaction(string operation, object payload, CancellationToken ct)
    {
        var tx = new Transaccion
        {
            Id = Guid.NewGuid(),
            Entidad = "PlanDeEstudio",
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