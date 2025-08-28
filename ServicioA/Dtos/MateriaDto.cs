namespace TAREATOPICOS.ServicioA.Dtos;

public class MateriaDto
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public int Creditos { get; set; }
        public int NivelId { get; set; }
    }
