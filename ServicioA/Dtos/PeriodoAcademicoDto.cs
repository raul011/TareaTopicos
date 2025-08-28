namespace TAREATOPICOS.ServicioA.Dtos;

public class PeriodoAcademicoDto
    {
        public int Id { get; set; }
        public string Gestion { get; set; } = null!;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
    }
