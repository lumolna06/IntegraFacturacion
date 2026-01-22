namespace IntegraPro.DTO.Models;

public class ResumenCxcDTO
{
    public decimal TotalPorCobrar { get; set; }
    public decimal TotalVencido { get; set; }
    public int CantidadFacturasPendientes { get; set; }
    public int ClientesMorosos { get; set; }
}