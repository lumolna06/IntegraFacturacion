namespace IntegraPro.DTO.Models;

public class MovimientoInventarioDTO
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public int UsuarioId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
    public string TipoMovimiento { get; set; } = "ENTRADA"; // ENTRADA o SALIDA
    public decimal Cantidad { get; set; }
    public decimal? ExistenciaPrevia { get; set; }
    public decimal? ExistenciaPosterior { get; set; }
    public string? DocumentoReferencia { get; set; }
    public string? Notas { get; set; }
}