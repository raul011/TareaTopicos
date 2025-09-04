using System.ComponentModel.DataAnnotations;

namespace   TAREATOPICOS.ServicioA.Dtos;

public class AulaDto
    {
        public int Id { get; set; }
        [Required]
        [StringLength(20)]
        public string Codigo { get; set; } = null!;
        [Range(1, 200)]
        public int Capacidad { get; set; }
        [Required]
        [StringLength(100)]
        public string Ubicacion { get; set; } = null!;
    }