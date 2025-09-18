namespace   TAREATOPICOS.ServicioA.Dtos;


public class GrupoMateriaCreateDto
{
    public string Grupo { get; set; }
    public int Cupo { get; set; }
    public string Estado { get; set; } = "ACTIVO";
    public string MateriaCodigo { get; set; }
    public string DocenteRegistro { get; set; }
    public string PeriodoGestion { get; set; }
    public int HorarioId { get; set; }
    public string AulaCodigo { get; set; }
}