using System.ComponentModel.DataAnnotations;

namespace TAREATOPICOS.ServicioA.Dtos;

public class PlanDeEstudioDto
    {
        public int Id { get; set; }
        [Required]
        [StringLength(150)]
        public string Nombre { get; set; } = null!;

        [StringLength(20)]
        public string? Codigo { get; set; }

        public DateTime Fecha { get; set; }
        public string Estado { get; set; } = "ACTIVO";

        [Required]
        public int CarreraId { get; set; }
    }