using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos.request;
using TAREATOPICOS.ServicioA.Dtos;

using TAREATOPICOS.ServicioA.Dtos.response;
using Microsoft.AspNetCore.Authorization;
using TAREATOPICOS.ServicioA.Services;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class EstudiantesController : ControllerBase
{
    private readonly ServicioAContext _context;
    private readonly IConfiguration _configuration;
    private readonly IBackgroundTaskQueue _queue;
    private readonly RedisTransaccionStore _store;

    public EstudiantesController(ServicioAContext context, IConfiguration configuration, IBackgroundTaskQueue queue, RedisTransaccionStore store)
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

    [HttpPost]
    public async Task<ActionResult<EstudianteRequestDto>> Create([FromBody] EstudianteRequestDto dto, CancellationToken ct)
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password); //  Hash de contraseña

        var e = new Estudiante
        {
            Registro = dto.Registro,
            Ci = dto.Ci,
            Nombre = dto.Nombre,
            Email = dto.Email,
            Telefono = dto.Telefono,
            Direccion = dto.Direccion,
            Estado = string.IsNullOrWhiteSpace(dto.Estado) ? "ACTIVO" : dto.Estado,
            CarreraId = dto.CarreraId,
            PasswordHash = passwordHash
        };
        _context.Estudiantes.Add(e);
        await _context.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = e.Id }, ToDTO(e));
    }

    // POST asincrónico: api/estudiantes/async
    [HttpPost("async")]
    public async Task<IActionResult> CreateAsync([FromBody] EstudianteRequestDto dto)
    {
        var transaccion = new Transaccion
        {
            TipoOperacion = "POST",
            Entidad = "Estudiante",
            Payload = JsonSerializer.Serialize(dto)
        };

        await _store.AddAsync(transaccion);
        _queue.Enqueue(transaccion);

        return AcceptedAtAction("GetEstado", "Transacciones", new { id = transaccion.Id }, new { id = transaccion.Id, estado = transaccion.Estado });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] EstudianteRequestDto dto, CancellationToken ct)
    {
        var e = await _context.Estudiantes.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return NotFound();

        e.Registro = dto.Registro;
        e.Ci = dto.Ci;
        e.Nombre = dto.Nombre;
        e.Email = dto.Email;
        e.Telefono = dto.Telefono;
        e.Direccion = dto.Direccion;
        e.Estado = dto.Estado;
        e.CarreraId = dto.CarreraId;

        if (!string.IsNullOrEmpty(dto.Password))
        {
            e.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        }

        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    // PUT asincrónico: api/estudiantes/async/{id}
    [HttpPut("async/{id:int}")]
    public async Task<IActionResult> UpdateAsync(int id, [FromBody] EstudianteRequestDto dto)
    {
        dto.Id = id;
        var transaccion = new Transaccion
        {
            TipoOperacion = "PUT",
            Entidad = "Estudiante",
            Payload = JsonSerializer.Serialize(dto)
        };

        await _store.AddAsync(transaccion);
        _queue.Enqueue(transaccion);

        return AcceptedAtAction("GetEstado", "Transacciones", new { id = transaccion.Id }, new { id = transaccion.Id, estado = transaccion.Estado });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var e = await _context.Estudiantes.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return NotFound();
        _context.Estudiantes.Remove(e);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    // DELETE asincrónico: api/estudiantes/async/{id}
    [HttpDelete("async/{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var transaccion = new Transaccion
        {
            TipoOperacion = "DELETE",
            Entidad = "Estudiante",
            Payload = JsonSerializer.Serialize(new { Id = id })
        };

        await _store.AddAsync(transaccion);
        _queue.Enqueue(transaccion);

        return AcceptedAtAction("GetEstado", "Transacciones", new { id = transaccion.Id }, new { id = transaccion.Id, estado = transaccion.Estado });
    }

    // ========== LOGIN ==========
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginDto login)
    {
        var estudiante = _context.Estudiantes
            .FirstOrDefault(e => e.Registro == login.Registro);

        if (estudiante == null || !BCrypt.Net.BCrypt.Verify(login.Password, estudiante.PasswordHash))
        {
            return Unauthorized("Registro o contraseña inválidos ❌");
        }

        var jwtKey = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("La clave JWT no está configurada.");

        var key = Encoding.UTF8.GetBytes(jwtKey);
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
