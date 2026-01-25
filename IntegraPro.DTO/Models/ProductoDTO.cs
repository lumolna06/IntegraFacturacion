namespace IntegraPro.DTO.Models;

public class ProductoDTO
{
    public int Id { get; set; }
    public int CategoriaId { get; set; }
    public string? CodCabys { get; set; }
    public string? CodigoBarras { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string UnidadMedida { get; set; } = "Unid";
    public decimal CostoActual { get; set; }

    public decimal Precio1 { get; set; }
    public decimal? Precio2 { get; set; }
    public decimal? Precio3 { get; set; }
    public decimal? Precio4 { get; set; }

    public decimal Existencia { get; set; }
    public decimal StockMinimo { get; set; }

    public bool ExentoIva { get; set; }
    public decimal PorcentajeImpuesto { get; set; } // Faltaba según tu SQL
    public bool Activo { get; set; }
    public bool EsServicio { get; set; }
    public bool EsElaborado { get; set; }

    // --- PROPIEDADES DE CONTEXTO ---
    // Esta es la que causaba el error CS1061
    public int SucursalId { get; set; }

    public List<ProductoComposicionDTO> Receta { get; set; } = new();
}

public class ProductoComposicionDTO
{
    public int MaterialId { get; set; }
    public string? MaterialNombre { get; set; }
    public decimal CantidadNecesaria { get; set; }
}