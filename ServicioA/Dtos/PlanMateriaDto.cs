using System.ComponentModel.DataAnnotations;

namespace TAREATOPICOS.ServicioA.Dtos;

public class PlanMateriaDto
    {
        public int Id { get; set; }

        [Required]
        public int PlanId { get; set; }

        [Required]
        public int MateriaId { get; set; }

        [Range(1, 12)]
        public int Semestre { get; set; }
    }
