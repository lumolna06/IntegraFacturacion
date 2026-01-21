namespace IntegraPro.DTO.Models;

public class ResumenCxpDTO
{
    public decimal TotalDeudaGlobal { get; set; }
    public int FacturasPendientes { get; set; }
    public int ProveedoresAQuienesSeDebe { get; set; }
    public decimal ProximoVencimientoMonto { get; set; }
    public DateTime? FechaProximoVencimiento { get; set; }
}