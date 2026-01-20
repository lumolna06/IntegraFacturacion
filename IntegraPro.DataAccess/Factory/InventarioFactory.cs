using IntegraPro.DataAccess.Dao;
using IntegraPro.DTO.Models;
using Microsoft.Data.SqlClient;

namespace IntegraPro.DataAccess.Factory;

public class InventarioFactory(string connectionString) : MasterDao(connectionString)
{
    public bool Insertar(MovimientoInventarioDTO dto)
    {
        // Eliminamos 'fecha' de la consulta para que SQL use el DEFAULT GETDATE()
        // Dejamos que el Trigger maneje las existencias previa y posterior
        string sql = @"INSERT INTO MOVIMIENTO_INVENTARIO 
                       (producto_id, usuario_id, tipo_movimiento, cantidad, documento_referencia, notas) 
                       VALUES (@pid, @uid, @tipo, @cant, @ref, @not)";

        var p = new[] {
            new SqlParameter("@pid", dto.ProductoId),
            new SqlParameter("@uid", dto.UsuarioId),
            new SqlParameter("@tipo", dto.TipoMovimiento.ToUpper()), // Normalizamos a MAYÚSCULAS para el Trigger
            new SqlParameter("@cant", dto.Cantidad),
            new SqlParameter("@ref", (object?)dto.DocumentoReferencia ?? DBNull.Value),
            new SqlParameter("@not", (object?)dto.Notas ?? DBNull.Value)
        };

        try
        {
            // Ejecutamos solo el INSERT. 
            // El Trigger se encargará de actualizar la tabla PRODUCTO y los saldos del movimiento.
            ExecuteNonQuery(sql, p, false);
            return true;
        }
        catch (Exception)
        {
            // Loguear la excepción si es necesario
            return false;
        }
    }
}