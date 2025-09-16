using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos.request;
using TAREATOPICOS.ServicioA.Dtos;

using TAREATOPICOS.ServicioA.Dtos.response;
using Microsoft.AspNetCore.Authorization;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class EstudiantesController : ControllerBase
{
    private readonly ServicioAContext _context;
    private readonly IConfiguration _configuration;

    public EstudiantesController(ServicioAContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
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

    [HttpGet("{registro}")]
    public async Task<ActionResult<EstudianteRequestDto>> Get(string registro, CancellationToken ct)
    {
        var e = await _context.Estudiantes.AsNoTracking().FirstOrDefaultAsync(x => x.Registro == registro, ct);
        return e is null ? NotFound() : Ok(ToDTO(e));
    }

    [HttpPost]
    public async Task<ActionResult<EstudianteRequestDto>> Create([FromBody] EstudianteRequestDto dto, CancellationToken ct)
    {
        // Validación: Asegurar que el registro y el email del estudiante sean únicos.
        if (await _context.Estudiantes.AnyAsync(e => e.Registro == dto.Registro, ct))
            return Conflict($"Ya existe un estudiante con el registro '{dto.Registro}'.");
        if (await _context.Estudiantes.AnyAsync(e => e.Email == dto.Email, ct))
            return Conflict($"Ya existe un estudiante con el email '{dto.Email}'.");

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
        return CreatedAtAction(nameof(Get), new { registro = e.Registro }, ToDTO(e));
    }

    [HttpPut("{registro}")]
    public async Task<IActionResult> Update(string registro, [FromBody] EstudianteRequestDto dto, CancellationToken ct)
    {
        var e = await _context.Estudiantes.FirstOrDefaultAsync(x => x.Registro == registro, ct);
        if (e is null) return NotFound();

        e.Ci = dto.Ci;
        e.Nombre = dto.Nombre;
        e.Email = dto.Email;
        e.Telefono = dto.Telefono;
        e.Direccion = dto.Direccion;
        e.Estado = dto.Estado;
        e.CarreraId = dto.CarreraId;
        // El registro del estudiante no debería cambiar.

        if (!string.IsNullOrEmpty(dto.Password))
        {
            e.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        }

        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{registro}")]
    public async Task<IActionResult> Delete(string registro, CancellationToken ct)
    {
        var e = await _context.Estudiantes.FirstOrDefaultAsync(x => x.Registro == registro, ct);
        if (e is null) return NotFound();
        _context.Estudiantes.Remove(e);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    // ========== LOGIN ==========
    [AllowAnonymous]
    [HttpPost("login")]
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

    #region Endpoints Asíncronos
    // POST: api/estudiantes/async
    [HttpPost("async")]
    public async Task<IActionResult> CreateAsync([FromBody] EstudianteRequestDto dto, CancellationToken ct)
    {
        dto.Id = 0;
        return await EnqueueTransaction("CREATE", dto, ct);
    }

    // PUT: api/estudiantes/async/{id}
    [HttpPut("async/{id:int}")]
    public async Task<IActionResult> UpdateAsync(int id, [FromBody] EstudianteRequestDto dto, CancellationToken ct)
    {
        dto.Id = id;
        return await EnqueueTransaction("UPDATE", dto, ct);
    }

    // DELETE: api/estudiantes/async/{id}
    [HttpDelete("async/{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken ct)
    {
        var dto = new EstudianteRequestDto { Id = id };
        return await EnqueueTransaction("DELETE", dto, ct);
    }
    #endregion

    #region Métodos Privados
    private async Task<IActionResult> EnqueueTransaction(string operation, object payload, CancellationToken ct)
    {
        var tx = new Transaccion
        {
            Id = Guid.NewGuid(),
            Entidad = "Estudiante",
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
