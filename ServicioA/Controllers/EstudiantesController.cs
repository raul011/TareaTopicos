using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Controllers
{
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

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto login)
        {
            var estudiante = _context.Estudiantes
                .FirstOrDefault(e => e.NumeroRegistro == login.NumeroRegistro);

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
                    new Claim("NumeroRegistro", estudiante.NumeroRegistro),
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
    }
}
