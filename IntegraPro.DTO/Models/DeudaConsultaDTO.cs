namespace IntegraPro.DTO.Models;

public class DeudaConsultaDTO
{
    public int DeudaId { get; set; }
    public int CompraId { get; set; }
    public string ProveedorCedula { get; set; } = string.Empty;
    public string ProveedorNombre { get; set; } = string.Empty;
    public string NumeroFactura { get; set; } = string.Empty;
    public DateTime FechaCompra { get; set; }
    public decimal MontoOriginal { get; set; }
    public decimal SaldoActual { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public string EstadoDeuda { get; set; } = string.Empty;
}