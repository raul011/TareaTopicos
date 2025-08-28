using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EstudiantesController : ControllerBase
{
    private readonly ServicioAContext _context;
    private readonly IConfiguration _configuration;

    public EstudiantesController(ServicioAContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    // ========== CRUD ==========
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EstudianteDto>>> GetAll(CancellationToken ct)
    {
        var list = await _context.Estudiantes.AsNoTracking().ToListAsync(ct);
        return Ok(list.Select(ToDTO));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EstudianteDto>> Get(int id, CancellationToken ct)
    {
        var e = await _context.Estudiantes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return e is null ? NotFound() : Ok(ToDTO(e));
    }

    [HttpPost]
    public async Task<ActionResult<EstudianteDto>> Create([FromBody] EstudianteDto dto, CancellationToken ct)
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password); // 🔑 Hash de contraseña

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

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] EstudianteDto dto, CancellationToken ct)
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

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var e = await _context.Estudiantes.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return NotFound();
        _context.Estudiantes.Remove(e);
        await _context.SaveChangesAsync(ct);
        return NoContent();
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

            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);
            var tokenHandler = new JwtSecurityTokenHandler();

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("NumeroRegistro", estudiante.Registro),
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
    private static EstudianteDto ToDTO(Estudiante e) => new()
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
}
