using IntegraPro.DataAccess.Dao;
using IntegraPro.DTO.Models;
using Microsoft.Data.SqlClient;

namespace IntegraPro.DataAccess.Factory;

public class InventarioFactory(string connectionString) : MasterDao(connectionString)
{
    public bool Insertar(MovimientoInventarioDTO dto)
    {
        string sql = @"INSERT INTO MOVIMIENTO_INVENTARIO 
            (producto_id, usuario_id, fecha, tipo_movimiento, cantidad, documento_referencia, notas) 
            VALUES (@pid, @uid, @fec, @tipo, @cant, @ref, @not)";

        var p = new[] {
            new SqlParameter("@pid", dto.ProductoId),
            new SqlParameter("@uid", dto.UsuarioId),
            new SqlParameter("@fec", dto.Fecha),
            new SqlParameter("@tipo", dto.TipoMovimiento),
            new SqlParameter("@cant", dto.Cantidad),
            new SqlParameter("@ref", (object?)dto.DocumentoReferencia ?? DBNull.Value),
            new SqlParameter("@not", (object?)dto.Notas ?? DBNull.Value)
        };

        try
        {
            // Cambiamos el return por una ejecución directa
            ExecuteNonQuery(sql, p, false);
            return true;
        }
        catch
        {
            // Si algo falla en el MasterDao, llegará aquí
            return false;
        }
    }
}