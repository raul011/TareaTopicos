namespace   TAREATOPICOS.ServicioA.Dtos;

public class InscripcionDto
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Estado { get; set; } = "PENDIENTE";
        public int EstudianteId { get; set; }
        public int PeriodoId { get; set; }
    }
