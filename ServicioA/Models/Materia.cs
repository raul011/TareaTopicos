namespace TAREATOPICOS.ServicioA.Models
{
    public class Materia
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int Creditos { get; set; }
        public ICollection<PlanMateria> PlanMaterias { get; set; } = new List<PlanMateria>();
    }
}
