namespace IntegraPro.DTO.Models;

public class MovimientoInventarioDTO
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public int UsuarioId { get; set; }

    // NUEVO: Propiedad necesaria para el control multi-sucursal
    public int SucursalId { get; set; }

    public DateTime Fecha { get; set; } = DateTime.Now;

    // ENTRADA, SALIDA o AJUSTE
    public string TipoMovimiento { get; set; } = "ENTRADA";

    public decimal Cantidad { get; set; }

    // Estos campos suelen ser calculados por el Trigger en la base de datos
    public decimal? ExistenciaPrevia { get; set; }
    public decimal? ExistenciaPosterior { get; set; }

    public string? DocumentoReferencia { get; set; }
    public string? Notas { get; set; }

    // PROPIEDADES DE NAVEGACIÓN (Opcionales: útiles para mostrar nombres en el historial)
    public string? ProductoNombre { get; set; }
    public string? UsuarioNombre { get; set; }
}