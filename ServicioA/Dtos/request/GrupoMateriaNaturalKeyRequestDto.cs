namespace TAREATOPICOS.ServicioA.Dtos.request
{
    public class GrupoMateriaNaturalKeyRequestDto
    {
        public string Grupo { get; set; } = string.Empty;
        public int Cupo { get; set; }
        public string? Estado { get; set; }
        public string MateriaCodigo { get; set; } = string.Empty;
        public string DocenteRegistro { get; set; } = string.Empty;
        public string PeriodoGestion { get; set; } = string.Empty;
        public int HorarioId { get; set; } // Horario no tiene clave natural, mantenemos Id
        public string AulaCodigo { get; set; } = string.Empty;
    }
}