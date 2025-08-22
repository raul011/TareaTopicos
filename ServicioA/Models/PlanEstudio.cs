namespace TAREATOPICOS.ServicioA.Models
{
    public class PlanEstudio
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int Anio { get; set; }
        public ICollection<PlanMateria> PlanMaterias { get; set; } = new List<PlanMateria>();
    }
}
