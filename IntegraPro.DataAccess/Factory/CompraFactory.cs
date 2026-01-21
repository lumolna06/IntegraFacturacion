using Microsoft.Data.SqlClient;
using IntegraPro.DTO.Models;
using System.Data;

namespace IntegraPro.DataAccess.Factory;

public class CompraFactory(string connectionString)
{
    private readonly string _connectionString = connectionString;

    public void ProcesarCompra(CompraDTO compra)
    {
        using var conn = new SqlConnection(_connectionString);
        conn.Open();
        using var trans = conn.BeginTransaction();
        try
        {
            // 1. Insertar Encabezado (Incluyendo TipoPago)
            string sqlEnc = @"INSERT INTO COMPRA_ENCABEZADO 
                (proveedor_id, sucursal_id, usuario_id, numero_factura_proveedor, fecha_compra, subtotal, total_impuestos, total_compra, notas, estado, tipo_pago) 
                VALUES (@prov, @suc, @usu, @num, @fecC, @sub, @imp, @tot, @not, 'Procesada', @tPag);
                SELECT SCOPE_IDENTITY();";

            using var cmdEnc = new SqlCommand(sqlEnc, conn, trans);
            cmdEnc.Parameters.AddWithValue("@prov", compra.ProveedorId);
            cmdEnc.Parameters.AddWithValue("@suc", compra.SucursalId);
            cmdEnc.Parameters.AddWithValue("@usu", compra.UsuarioId);
            cmdEnc.Parameters.AddWithValue("@num", (object?)compra.NumeroFacturaProveedor ?? DBNull.Value);
            cmdEnc.Parameters.AddWithValue("@fecC", compra.FechaCompra);
            cmdEnc.Parameters.AddWithValue("@sub", compra.Subtotal);
            cmdEnc.Parameters.AddWithValue("@imp", compra.TotalImpuestos);
            cmdEnc.Parameters.AddWithValue("@tot", compra.TotalCompra);
            cmdEnc.Parameters.AddWithValue("@not", (object?)compra.Notas ?? DBNull.Value);
            cmdEnc.Parameters.AddWithValue("@tPag", compra.TipoPago);

            int compraId = Convert.ToInt32(cmdEnc.ExecuteScalar());

            // 2. Lógica de Cuenta por Pagar (CXP) si es crédito
            if (compra.TipoPago == "Credito")
            {
                string sqlCxp = @"INSERT INTO CUENTA_PAGAR 
                    (compra_id, proveedor_id, monto_total, saldo_pendiente, fecha_vencimiento, estado) 
                    VALUES (@cId, @pId, @tot, @tot, @fVen, 'Pendiente')";

                using var cmdCxp = new SqlCommand(sqlCxp, conn, trans);
                cmdCxp.Parameters.AddWithValue("@cId", compraId);
                cmdCxp.Parameters.AddWithValue("@pId", compra.ProveedorId);
                cmdCxp.Parameters.AddWithValue("@tot", compra.TotalCompra);
                cmdCxp.Parameters.AddWithValue("@fVen", compra.FechaCompra.AddDays(compra.DiasCredito));
                cmdCxp.ExecuteNonQuery();
            }

            // 3. Insertar Detalles, Inventario y Actualizar Costos
            foreach (var det in compra.Detalles)
            {
                string sqlDet = "INSERT INTO COMPRA_DETALLE (compra_id, producto_id, cantidad, costo_unitario_neto, monto_impuesto, total_linea) VALUES (@cId, @pId, @can, @cos, @mImp, @totL)";
                using var cmdDet = new SqlCommand(sqlDet, conn, trans);
                cmdDet.Parameters.AddWithValue("@cId", compraId);
                cmdDet.Parameters.AddWithValue("@pId", det.ProductoId);
                cmdDet.Parameters.AddWithValue("@can", det.Cantidad);
                cmdDet.Parameters.AddWithValue("@cos", det.CostoUnitarioNeto);
                cmdDet.Parameters.AddWithValue("@mImp", det.MontoImpuesto);
                cmdDet.Parameters.AddWithValue("@totL", det.TotalLinea);
                cmdDet.ExecuteNonQuery();

                string sqlMov = "INSERT INTO MOVIMIENTO_INVENTARIO (producto_id, usuario_id, fecha, tipo_movimiento, cantidad, documento_referencia, notas) VALUES (@pId, @uId, GETDATE(), 'Entrada', @can, @ref, 'Compra Recibida')";
                using var cmdMov = new SqlCommand(sqlMov, conn, trans);
                cmdMov.Parameters.AddWithValue("@pId", det.ProductoId);
                cmdMov.Parameters.AddWithValue("@uId", compra.UsuarioId);
                cmdMov.Parameters.AddWithValue("@can", det.Cantidad);
                cmdMov.Parameters.AddWithValue("@ref", "FAC-" + compra.NumeroFacturaProveedor);
                cmdMov.ExecuteNonQuery();

                string sqlCosto = "UPDATE PRODUCTO SET costo_actual = @nCos WHERE id = @pId";
                using var cmdCosto = new SqlCommand(sqlCosto, conn, trans);
                cmdCosto.Parameters.AddWithValue("@nCos", det.CostoUnitarioNeto);
                cmdCosto.Parameters.AddWithValue("@pId", det.ProductoId);
                cmdCosto.ExecuteNonQuery();
            }

            trans.Commit();
        }
        catch { trans.Rollback(); throw; }
    }

    public void AnularCompra(int compraId, int usuarioId)
    {
        using var conn = new SqlConnection(_connectionString);
        conn.Open();
        using var trans = conn.BeginTransaction();
        try
        {
            var detalles = new List<(int pId, decimal cant, string refProv)>();
            using (var cmdGet = new SqlCommand("SELECT D.producto_id, D.cantidad, E.numero_factura_proveedor FROM COMPRA_DETALLE D JOIN COMPRA_ENCABEZADO E ON D.compra_id = E.id WHERE E.id = @id AND E.estado = 'Procesada'", conn, trans))
            {
                cmdGet.Parameters.AddWithValue("@id", compraId);
                using var reader = cmdGet.ExecuteReader();
                while (reader.Read()) detalles.Add(((int)reader[0], (decimal)reader[1], reader[2].ToString()!));
            }

            if (detalles.Count == 0) throw new Exception("Compra no encontrada o ya está anulada.");

            foreach (var det in detalles)
            {
                string sqlMov = "INSERT INTO MOVIMIENTO_INVENTARIO (producto_id, usuario_id, fecha, tipo_movimiento, cantidad, documento_referencia, notas) VALUES (@pId, @uId, GETDATE(), 'Salida', @can, @ref, 'ANULACIÓN DE COMPRA')";
                using var cmdMov = new SqlCommand(sqlMov, conn, trans);
                cmdMov.Parameters.AddWithValue("@pId", det.pId);
                cmdMov.Parameters.AddWithValue("@uId", usuarioId);
                cmdMov.Parameters.AddWithValue("@can", det.cant);
                cmdMov.Parameters.AddWithValue("@ref", "ANUL-" + det.refProv);
                cmdMov.ExecuteNonQuery();
            }

            string sqlAnuCxp = "UPDATE CUENTA_PAGAR SET estado = 'Anulado', saldo_pendiente = 0 WHERE compra_id = @cId";
            using var cmdAnuCxp = new SqlCommand(sqlAnuCxp, conn, trans);
            cmdAnuCxp.Parameters.AddWithValue("@cId", compraId);
            cmdAnuCxp.ExecuteNonQuery();

            using (var cmdUp = new SqlCommand("UPDATE COMPRA_ENCABEZADO SET estado = 'Anulada' WHERE id = @id", conn, trans))
            {
                cmdUp.Parameters.AddWithValue("@id", compraId);
                cmdUp.ExecuteNonQuery();
            }

            trans.Commit();
        }
        catch { trans.Rollback(); throw; }
    }

    // --- NUEVO: ALERTAS DE CUENTAS POR PAGAR ---
    public List<AlertaPagoDTO> ObtenerAlertasPagos()
    {
        var alertas = new List<AlertaPagoDTO>();
        using var conn = new SqlConnection(_connectionString);
        // Consumimos la vista SQL que creamos anteriormente
        using var cmd = new SqlCommand("SELECT * FROM VW_ALERTA_CUENTAS_PAGAR_PROXIMAS ORDER BY fecha_vencimiento ASC", conn);

        conn.Open();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            alertas.Add(new AlertaPagoDTO
            {
                Id = (int)reader["id"],
                ProveedorNombre = reader["proveedor_nombre"].ToString()!,
                SaldoPendiente = (decimal)reader["saldo_pendiente"],
                FechaVencimiento = (DateTime)reader["fecha_vencimiento"],
                DiasParaVencimiento = (int)reader["dias_para_vencimiento"],
                Urgencia = reader["urgencia"].ToString()!
            });
        }
        return alertas;
    }
}