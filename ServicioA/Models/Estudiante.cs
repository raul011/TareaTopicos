namespace TAREATOPICOS.ServicioA.Models
{
    public class Estudiante
    {
        public int Id { get; set; }
        public string NumeroRegistro { get; set; } = string.Empty;
        public string CarnetIdentidad { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string? Correo { get; set; }
    }
}
