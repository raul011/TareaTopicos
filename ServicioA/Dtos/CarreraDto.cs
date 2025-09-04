using System.ComponentModel.DataAnnotations;

namespace TAREATOPICOS.ServicioA.Dtos;

public class CarreraDto
{
    public int Id { get; set; }
    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = null!;
}
