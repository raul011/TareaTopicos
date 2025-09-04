using System.ComponentModel.DataAnnotations;

namespace TAREATOPICOS.ServicioA.Dtos;

public class HistorialAcademicoDto
    {
        public int Id { get; set; }

        [Range(1, 10)]
        public int Intento { get; set; } = 1;

        [Range(0, 100)]
        public decimal? UltimaNota { get; set; }
        public bool Aprobado { get; set; } = false;
        public DateTime? FechaAprobacion { get; set; }

        [Required]
        public int DetalleInscripcionId { get; set; }
    }
