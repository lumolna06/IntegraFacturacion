namespace IntegraPro.DTO.Models;

public class XmlPreprocesoDTO
{
    public string? NumeroFactura { get; set; }
    public DateTime FechaEmision { get; set; }
    public string? ProveedorCedula { get; set; }
    public string? ProveedorNombre { get; set; }

    // Propiedad añadida para vincular con el ID interno de la DB
    public int ProveedorId { get; set; }

    public List<LineaPreprocesoDTO> Lineas { get; set; } = new();
}

public class LineaPreprocesoDTO
{
    public string? DetalleXml { get; set; }
    public string? CodigoCabys { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal MontoImpuesto { get; set; }
    public decimal TotalLinea { get; set; }
    public int? ProductoIdSugerido { get; set; }
    public string? ProductoNombreSugerido { get; set; }
}