namespace TAREATOPICOS.ServicioA.Models
{
    public class PlanMateria
    {
        public int Id { get; set; }
        public int PlanId { get; set; }
        public int MateriaId { get; set; }
        public int Semestre { get; set; }

        public PlanEstudio Plan { get; set; } = default!;
        public Materia Materia { get; set; } = default!;
    }
}
