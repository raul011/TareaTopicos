using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos.request;
using Microsoft.AspNetCore.Authorization;
using TAREATOPICOS.ServicioA.Services;
using System.Text.Json;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PeriodosAcademicosController : ControllerBase
{
    private readonly ServicioAContext _context;
    private readonly IBackgroundTaskQueue _queue;
    private readonly ITransaccionStore _store;

    public PeriodosAcademicosController(ServicioAContext context, IBackgroundTaskQueue queue, ITransaccionStore store)
    {
        _context = context;
        _queue = queue;
        _store = store;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PeriodoAcademicoRequestDto>>> GetAll(CancellationToken ct)
    {
        var list = await _context.PeriodosAcademicos.AsNoTracking().OrderByDescending(p => p.FechaInicio).ToListAsync(ct);
        return Ok(list.Select(ToDTO));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PeriodoAcademicoRequestDto>> Get(int id, CancellationToken ct)
    {
        var p = await _context.PeriodosAcademicos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return p is null ? NotFound() : Ok(ToDTO(p));
    }

    [HttpPost]
    public async Task<ActionResult<PeriodoAcademicoRequestDto>> Create([FromBody] PeriodoAcademicoRequestDto dto, CancellationToken ct)
    {
        if (dto.FechaFin < dto.FechaInicio) return BadRequest("La fecha fin no puede ser anterior a la fecha inicio.");

        var p = new PeriodoAcademico { Gestion = dto.Gestion, FechaInicio = dto.FechaInicio, FechaFin = dto.FechaFin };
        _context.PeriodosAcademicos.Add(p);
        await _context.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = p.Id }, ToDTO(p));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] PeriodoAcademicoRequestDto dto, CancellationToken ct)
    {
        var p = await _context.PeriodosAcademicos.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return NotFound();
        if (dto.FechaFin < dto.FechaInicio) return BadRequest("La fecha fin no puede ser anterior a la fecha inicio.");

        p.Gestion = dto.Gestion;
        p.FechaInicio = dto.FechaInicio;
        p.FechaFin = dto.FechaFin;
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var p = await _context.PeriodosAcademicos.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return NotFound();
        _context.PeriodosAcademicos.Remove(p);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    #region Endpoints Asíncronos
    // POST: api/periodosacademicos/async
    [HttpPost("async")]
    public async Task<IActionResult> CreateAsync([FromBody] PeriodoAcademicoRequestDto dto, CancellationToken ct)
    {
        dto.Id = 0;
        return await EnqueueTransaction("CREATE", dto, ct);
    }

    // PUT: api/periodosacademicos/async/{id}
    [HttpPut("async/{id:int}")]
    public async Task<IActionResult> UpdateAsync(int id, [FromBody] PeriodoAcademicoRequestDto dto, CancellationToken ct)
    {
        dto.Id = id;
        return await EnqueueTransaction("UPDATE", dto, ct);
    }

    // DELETE: api/periodosacademicos/async/{id}
    [HttpDelete("async/{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken ct)
    {
        var dto = new PeriodoAcademicoRequestDto { Id = id };
        return await EnqueueTransaction("DELETE", dto, ct);
    }
    #endregion

    private async Task<IActionResult> EnqueueTransaction(string operation, object payload, CancellationToken ct)
    {
        var tx = new Transaccion
        {
            Id = Guid.NewGuid(),
            Entidad = "PeriodoAcademico",
            TipoOperacion = operation,
            Payload = JsonSerializer.Serialize(payload),
            Estado = "EN_COLA",
            NotBefore = DateTimeOffset.UtcNow
        };
        await _store.AddAsync(tx, ct);
        await _queue.EnqueueAsync(tx, "default", ct);
        return AcceptedAtAction(nameof(TransaccionesController.Get), "Transacciones", new { id = tx.Id }, new { transaccionId = tx.Id, estado = tx.Estado });
    }

    private static PeriodoAcademicoRequestDto ToDTO(PeriodoAcademico p) => new()
    {
        Id = p.Id,
        Gestion = p.Gestion,
        FechaInicio = p.FechaInicio,
        FechaFin = p.FechaFin
    };
}
