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
            // Agregamos el mapeo de SucursalId para que no salga en 0
            SucursalId = row.Table.Columns.Contains("sucursal_id") && row["sucursal_id"] != DBNull.Value
                         ? Convert.ToInt32(row["sucursal_id"])
                         : 0,

            CategoriaId = Convert.ToInt32(row["categoria_id"]),
            CodCabys = row["cod_cabys"]?.ToString(),
            CodigoBarras = row["codigo_barras"]?.ToString(),
            Nombre = row["nombre"].ToString() ?? "",
            Descripcion = row["descripcion"]?.ToString(),
            UnidadMedida = row["unidad_medida"].ToString() ?? "Unid",
            CostoActual = Convert.ToDecimal(row["costo_actual"]),
            Precio1 = Convert.ToDecimal(row["precio_1"]),

            Precio2 = row["precio_2"] != DBNull.Value ? Convert.ToDecimal(row["precio_2"]) : null,
            Precio3 = row["precio_3"] != DBNull.Value ? Convert.ToDecimal(row["precio_3"]) : null,
            Precio4 = row["precio_4"] != DBNull.Value ? Convert.ToDecimal(row["precio_4"]) : null,

            Existencia = row.Table.Columns.Contains("existencia_local")
                         ? Convert.ToDecimal(row["existencia_local"])
                         : Convert.ToDecimal(row["existencia"]),

            StockMinimo = Convert.ToDecimal(row["stock_minimo"]),
            ExentoIva = Convert.ToBoolean(row["exento_iva"]),

            PorcentajeImpuesto = row.Table.Columns.Contains("porcentaje_impuesto")
                                 ? Convert.ToDecimal(row["porcentaje_impuesto"])
                                 : 0,

            EsServicio = Convert.ToBoolean(row["es_servicio"]),
            EsElaborado = Convert.ToBoolean(row["es_elaborado"]),
            Activo = Convert.ToBoolean(row["activo"])

        };
    }

    public SqlParameter[] MapToParameters(ProductoDTO entity)
    {
        var parameters = new List<SqlParameter>
        {
            new SqlParameter("@categoria_id", entity.CategoriaId),
            new SqlParameter("@cod_cabys", (object?)entity.CodCabys ?? DBNull.Value),
            new SqlParameter("@codigo_barras", (object?)entity.CodigoBarras ?? DBNull.Value),
            new SqlParameter("@nombre", entity.Nombre),
            new SqlParameter("@descripcion", (object?)entity.Descripcion ?? DBNull.Value),
            new SqlParameter("@unidad_medida", entity.UnidadMedida),
            new SqlParameter("@costo_actual", entity.CostoActual),
            new SqlParameter("@precio_1", entity.Precio1),
            new SqlParameter("@precio_2", (object?)entity.Precio2 ?? DBNull.Value),
            new SqlParameter("@precio_3", (object?)entity.Precio3 ?? DBNull.Value),
            new SqlParameter("@precio_4", (object?)entity.Precio4 ?? DBNull.Value),
            new SqlParameter("@exento_iva", entity.ExentoIva),
            new SqlParameter("@porcentaje_impuesto", entity.PorcentajeImpuesto), // Agregado
            new SqlParameter("@existencia", entity.Existencia),
            new SqlParameter("@stock_minimo", entity.StockMinimo),
            new SqlParameter("@es_servicio", entity.EsServicio),
            new SqlParameter("@es_elaborado", entity.EsElaborado),
            new SqlParameter("@activo", entity.Activo),
            new SqlParameter("@sucursal_id", entity.SucursalId) // Agregado para el contexto de sucursal
        };

        if (entity.Id > 0)
        {
            parameters.Add(new SqlParameter("@id", entity.Id));
        }

        return parameters.ToArray();
    }
}