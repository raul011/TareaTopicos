namespace TAREATOPICOS.ServicioA.Dtos;

public class EstudianteDto
    {
        public int Id { get; set; }
        public string Registro { get; set; } = null!;
        public string Ci { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
        public string Estado { get; set; } = "ACTIVO";
        public int CarreraId { get; set; }
    }
