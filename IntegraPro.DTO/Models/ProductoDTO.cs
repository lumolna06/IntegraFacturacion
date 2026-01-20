namespace IntegraPro.DTO.Models;

public class ProductoDTO
{
    public int Id { get; set; }
    public int CategoriaId { get; set; }
    public string? CodCabys { get; set; }
    public string? CodigoBarras { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string UnidadMedida { get; set; } = "Unid"; // Unid, Kg, M2, M
    public decimal CostoActual { get; set; }
    public decimal Precio1 { get; set; }
    public decimal Existencia { get; set; }
    public decimal StockMinimo { get; set; }
    public bool EsServicio { get; set; }
    public bool EsElaborado { get; set; } // Si es TRUE, usa receta
    public bool Activo { get; set; }

    // Propiedad para manejar la receta/composición
    public List<ProductoComposicionDTO> Receta { get; set; } = new();
}

public class ProductoComposicionDTO
{
    public int MaterialId { get; set; }
    public string? MaterialNombre { get; set; }
    public decimal CantidadNecesaria { get; set; }
}