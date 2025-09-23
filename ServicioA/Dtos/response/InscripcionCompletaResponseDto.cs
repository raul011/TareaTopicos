namespace TAREATOPICOS.ServicioA.Dtos.response;

public class InscripcionCompletaResponseDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public string Estado { get; set; } = "PENDIENTE";
    public string? Observaciones { get; set; }
    public EstudianteResponseDto Estudiante { get; set; } = null!;
    public PeriodoAcademicoResponseDto Periodo { get; set; } = null!;
    public List<DetalleInscripcionCompletoDto> Detalles { get; set; } = new List<DetalleInscripcionCompletoDto>();
    public List<string> Errores { get; set; } = new List<string>();
    public bool Exito { get; set; } = true;
}

public class DetalleInscripcionCompletoDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = null!;
    public string Estado { get; set; } = "INSCRITO";
    public string MateriaNombre { get; set; } = null!;
    public string Grupo { get; set; } = null!;
    public string DocenteNombre { get; set; } = null!;
    public string? Horario { get; set; }
    public string? Aula { get; set; }
    public int CupoDisponible { get; set; }
    public string? Observaciones { get; set; }
}
