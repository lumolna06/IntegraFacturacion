using System.Data;
using IntegraPro.DTO.Models;
using Microsoft.Data.SqlClient;

namespace IntegraPro.DataAccess.Mappers;

public class ProductoMapper : IMapper<ProductoDTO>
{
    public ProductoDTO MapFromRow(DataRow row)
    {
        return new ProductoDTO
        {
            Id = Convert.ToInt32(row["id"]),
            CategoriaId = Convert.ToInt32(row["categoria_id"]),
            CodCabys = row["cod_cabys"]?.ToString(),
            CodigoBarras = row["codigo_barras"]?.ToString(),
            Nombre = row["nombre"].ToString() ?? "",
            Descripcion = row["descripcion"]?.ToString(),
            UnidadMedida = row["unidad_medida"].ToString() ?? "Unid",
            CostoActual = Convert.ToDecimal(row["costo_actual"]),
            Precio1 = Convert.ToDecimal(row["precio_1"]),
            Existencia = Convert.ToDecimal(row["existencia"]),
            StockMinimo = Convert.ToDecimal(row["stock_minimo"]),
            EsServicio = Convert.ToBoolean(row["es_servicio"]),
            EsElaborado = Convert.ToBoolean(row["es_elaborado"]),
            Activo = Convert.ToBoolean(row["activo"])
        };
    }

    public SqlParameter[] MapToParameters(ProductoDTO entity)
    {
        var parameters = new List<SqlParameter>
        {
            // Nota: Quitamos el @id de la lista base
            new SqlParameter("@categoria_id", entity.CategoriaId),
            new SqlParameter("@cod_cabys", (object?)entity.CodCabys ?? DBNull.Value),
            new SqlParameter("@codigo_barras", (object?)entity.CodigoBarras ?? DBNull.Value),
            new SqlParameter("@nombre", entity.Nombre),
            new SqlParameter("@descripcion", (object?)entity.Descripcion ?? DBNull.Value),
            new SqlParameter("@unidad_medida", entity.UnidadMedida),
            new SqlParameter("@costo_actual", entity.CostoActual),
            new SqlParameter("@precio_1", entity.Precio1),
            new SqlParameter("@existencia", entity.Existencia),
            new SqlParameter("@stock_minimo", entity.StockMinimo),
            new SqlParameter("@es_servicio", entity.EsServicio),
            new SqlParameter("@es_elaborado", entity.EsElaborado),
            new SqlParameter("@activo", entity.Activo)
        };

        // SOLO si el Id es mayor a 0 (es un Update), lo agregamos.
        // sp_Producto_Insert fallará si recibe @id porque no está en su definición.
        if (entity.Id > 0)
        {
            parameters.Add(new SqlParameter("@id", entity.Id));
        }

        return parameters.ToArray();
    }
}