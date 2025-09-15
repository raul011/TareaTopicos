using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos;
using TAREATOPICOS.ServicioA.Services;
using Microsoft.AspNetCore.Authorization;

namespace TAREATOPICOS.ServicioA.Controllers.Sincrono;

[ApiController]
[Route("api/[controller]")]
// [Authorize]
public class DocentesAsyncController : ControllerBase
{
    private readonly ServicioAContext _context;
    private readonly IBackgroundTaskQueue _queue;
    private readonly ITransaccionStore _store;

    public DocentesAsyncController(ServicioAContext context, IBackgroundTaskQueue queue, ITransaccionStore store)
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
    [HttpPost("async")]
    public async Task<IActionResult> CreateAsync([FromBody] DocenteDto dto, CancellationToken ct = default)
    {
        if (await _context.Docentes.AnyAsync(d => d.Registro == dto.Registro, ct))
            return Conflict(new { mensaje = $"El registro de docente '{dto.Registro}' ya existe." });

        var tx = new Transaccion
        {
            Entidad = "Docente",
            TipoOperacion = "CrearDocente",
            Payload = JsonSerializer.Serialize(dto),
            Estado = "EN_COLA"
        };

        await _store.AddAsync(tx);
        await _queue.EnqueueAsync(tx);

        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    // PUT: api/docentes/{id}
    [HttpPut("async/{id:int}")]
    public async Task<IActionResult> UpdateAsync(int id, [FromBody] DocenteDto dto, CancellationToken ct = default)
    {
        if (!await _context.Docentes.AnyAsync(d => d.Id == id, ct))
            return NotFound(new { mensaje = "El docente no existe." });

        dto.Id = id;

        var tx = new Transaccion
        {
            Entidad = "Docente",
            TipoOperacion = "ActualizarDocente",
            Payload = JsonSerializer.Serialize(dto),
            Estado = "EN_COLA"
        };

        await _store.AddAsync(tx);
        await _queue.EnqueueAsync(tx);

        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    // DELETE: api/docentes/{id}
    [HttpDelete("async/{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken ct = default)
    {
        if (!await _context.Docentes.AnyAsync(d => d.Id == id, ct))
            return NotFound(new { mensaje = "El docente no existe." });

        var payload = new { Id = id };

        var tx = new Transaccion
        {
            Entidad = "Docente",
            TipoOperacion = "EliminarDocente",
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