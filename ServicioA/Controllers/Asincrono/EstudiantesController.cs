using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Services;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos.request;
using TAREATOPICOS.ServicioA.Dtos;
using TAREATOPICOS.ServicioA.Dtos.response;
using Microsoft.AspNetCore.Authorization;

namespace TAREATOPICOS.ServicioA.Controllers.Sincrono;

[ApiController]
[Route("api/[controller]")]
// [AllowAnonymous]
public class EstudiantesAsyncController : ControllerBase
{
    private readonly ServicioAContext _context;
    private readonly IConfiguration _configuration;
    private readonly IBackgroundTaskQueue _queue;
    private readonly ITransaccionStore _store;

    public EstudiantesAsyncController(ServicioAContext context, IConfiguration configuration, IBackgroundTaskQueue queue, ITransaccionStore store)
    {
        _context = context;
        _configuration = configuration;
        _queue = queue;
        _store = store;
    }

    // --------- CRUD ------------------
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EstudianteResponseDto>>> GetAll(CancellationToken ct)
    {
        var list = await _context.Estudiantes
            .AsNoTracking()
            .Include(e => e.Carrera) // Incluir la carrera para poder mapearla
            .ToListAsync(ct);
        return Ok(list.Select(ToResponseDTO));
    }


    [HttpGet("{id:int}")]
    public async Task<ActionResult<EstudianteRequestDto>> Get(int id, CancellationToken ct)
    {
        var e = await _context.Estudiantes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return e is null ? NotFound() : Ok(ToDTO(e));
    }

    [HttpPost("async")]
    public async Task<IActionResult> CreateAsync([FromBody] EstudianteRequestDto dto, CancellationToken ct)
    {
        if (await _context.Estudiantes.AnyAsync(e => e.Registro == dto.Registro, ct))
            return Conflict(new { mensaje = $"El registro de estudiante '{dto.Registro}' ya existe." });

        var tx = new Transaccion
        {
            Entidad = "Estudiante",
            TipoOperacion = "CrearEstudiante",
            Payload = JsonSerializer.Serialize(dto),
            Estado = "EN_COLA"
        };

        await _store.AddAsync(tx);
        await _queue.EnqueueAsync(tx);

        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    [HttpPut("async/{id:int}")]
    public async Task<IActionResult> UpdateAsync(int id, [FromBody] EstudianteRequestDto dto, CancellationToken ct)
    {
        if (!await _context.Estudiantes.AnyAsync(e => e.Id == id, ct))
            return NotFound(new { mensaje = "El estudiante no existe." });

        dto.Id = id;

        var tx = new Transaccion
        {
            Entidad = "Estudiante",
            TipoOperacion = "ActualizarEstudiante",
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
        if (!await _context.Estudiantes.AnyAsync(e => e.Id == id, ct))
            return NotFound(new { mensaje = "El estudiante no existe." });

        var payload = new { Id = id };

        var tx = new Transaccion
        {
            Entidad = "Estudiante",
            TipoOperacion = "EliminarEstudiante",
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

    // ========== LOGIN ==========
    [HttpPost("login")]
    [AllowAnonymous] // Login debe ser anónimo
    public IActionResult Login([FromBody] LoginDto login)
    {
        var estudiante = _context.Estudiantes
            .FirstOrDefault(e => e.Registro == login.Registro);

        if (estudiante == null || !BCrypt.Net.BCrypt.Verify(login.Password, estudiante.PasswordHash))
        {
            return Unauthorized("Registro o contraseña inválidos ❌");
        }

        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);
        var tokenHandler = new JwtSecurityTokenHandler();

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                    new Claim("Registro", estudiante.Registro),
                    new Claim("Nombre", estudiante.Nombre)
                }),
            Expires = DateTime.UtcNow.AddHours(2),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature
            ),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"]
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwt = tokenHandler.WriteToken(token);

        return Ok(new { token = jwt });
    }


    // ====== Mapper ======
    private static EstudianteRequestDto ToDTO(Estudiante e) => new()
    {
        Id = e.Id,
        Registro = e.Registro,
        Ci = e.Ci,
        Nombre = e.Nombre,
        Email = e.Email,
        Telefono = e.Telefono,
        Direccion = e.Direccion,
        Estado = e.Estado,
        CarreraId = e.CarreraId
    };

    private static EstudianteResponseDto ToResponseDTO(Estudiante e) => new()
    {
        Registro = e.Registro,
        Ci = e.Ci,
        Nombre = e.Nombre,
        Email = e.Email,
        Telefono = e.Telefono,
        Direccion = e.Direccion,
        Estado = e.Estado,
        Carrera = new CarreraDto
        {
            Id = e.Carrera.Id,
            Nombre = e.Carrera.Nombre
        }
    };
}
