namespace TAREATOPICOS.ServicioA.Dtos;

public class PlanEstudioDto
{
    public int Id { get; set; }
    public required string Nombre { get; set; }
    public int Anio { get; set; }
    public List<MateriaDto> Materias { get; set; } = new();
}

