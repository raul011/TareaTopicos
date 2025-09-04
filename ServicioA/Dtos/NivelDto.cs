using System.ComponentModel.DataAnnotations;

namespace TAREATOPICOS.ServicioA.Dtos;

public class NivelDto
{
    public int Id { get; set; }
    [Range(1, 20)]
    public int Numero { get; set; }

    [Required]
    [StringLength(50)]
    public string Nombre { get; set; } = null!;
}
