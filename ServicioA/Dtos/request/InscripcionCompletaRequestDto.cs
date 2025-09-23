namespace TAREATOPICOS.ServicioA.Dtos.request;

public class InscripcionCompletaRequestDto
{
    public int EstudianteId { get; set; }
    public int PeriodoId { get; set; }
    public List<int> GrupoMateriaIds { get; set; } = new List<int>();
    public string? Observaciones { get; set; }
}

public class DetalleInscripcionCreateDto
{
    public int GrupoMateriaId { get; set; }
    public string? Observaciones { get; set; }
}
