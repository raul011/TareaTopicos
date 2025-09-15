using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos.request;
using TAREATOPICOS.ServicioA.Services;
using Microsoft.AspNetCore.Authorization;

namespace TAREATOPICOS.ServicioA.Controllers.Sincrono;

[ApiController]
[Route("api/[controller]")]
// [Authorize]
public class PeriodosAcademicosAsyncController : ControllerBase
{
    private readonly ServicioAContext _context;
    private readonly IBackgroundTaskQueue _queue;
    private readonly ITransaccionStore _store;

    public PeriodosAcademicosAsyncController(ServicioAContext context, IBackgroundTaskQueue queue, ITransaccionStore store)
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

    [HttpPost("async")]
    public async Task<IActionResult> CreateAsync([FromBody] PeriodoAcademicoRequestDto dto, CancellationToken ct)
    {
        if (dto.FechaFin < dto.FechaInicio) return BadRequest("La fecha fin no puede ser anterior a la fecha inicio.");
        if (await _context.PeriodosAcademicos.AnyAsync(p => p.Gestion == dto.Gestion, ct))
            return Conflict(new { mensaje = $"La gestión '{dto.Gestion}' ya existe." });

        var tx = new Transaccion
        {
            Entidad = "PeriodoAcademico",
            TipoOperacion = "CrearPeriodo",
            Payload = JsonSerializer.Serialize(dto),
            Estado = "EN_COLA"
        };

        await _store.AddAsync(tx);
        await _queue.EnqueueAsync(tx);

        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    [HttpPut("async/{id:int}")]
    public async Task<IActionResult> UpdateAsync(int id, [FromBody] PeriodoAcademicoRequestDto dto, CancellationToken ct)
    {
        if (!await _context.PeriodosAcademicos.AnyAsync(p => p.Id == id, ct))
            return NotFound(new { mensaje = "El período académico no existe." });
        if (dto.FechaFin < dto.FechaInicio) return BadRequest("La fecha fin no puede ser anterior a la fecha inicio.");

        dto.Id = id;

        var tx = new Transaccion
        {
            Entidad = "PeriodoAcademico",
            TipoOperacion = "ActualizarPeriodo",
            Payload = JsonSerializer.Serialize(dto),
            Estado = "EN_COLA"
        };

        await _store.AddAsync(tx);
        await _queue.EnqueueAsync(tx);

        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    [HttpDelete("async/{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken ct)
    {
        if (!await _context.PeriodosAcademicos.AnyAsync(p => p.Id == id, ct))
            return NotFound(new { mensaje = "El período académico no existe." });

        var payload = new { Id = id };

        var tx = new Transaccion
        {
            Entidad = "PeriodoAcademico",
            TipoOperacion = "EliminarPeriodo",
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

    private static PeriodoAcademicoRequestDto ToDTO(PeriodoAcademico p) => new()
    {
        Id = p.Id,
        Gestion = p.Gestion,
        FechaInicio = p.FechaInicio,
        FechaFin = p.FechaFin
    };
}
