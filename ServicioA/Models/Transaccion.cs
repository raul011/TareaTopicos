
public class Transaccion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TipoOperacion { get; set; } = "";  
    public string Entidad { get; set; } = "";        
    public string? Payload { get; set; }            
    public string Estado { get; set; } = "EN_COLA";
    
    public int Attempt { get; set; } = 0;
    public int Priority { get; set; } = 1;             // 0 alta, 1 normal, 2 baja
    public int MaxRetries { get; set; } = 3;
    public DateTimeOffset NotBefore { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
// registramos todas las transacciones