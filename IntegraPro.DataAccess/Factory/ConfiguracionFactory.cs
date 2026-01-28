using IntegraPro.DataAccess.Dao;
using IntegraPro.DTO.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace IntegraPro.DataAccess.Factory;

public class ConfiguracionFactory(string connectionString) : MasterDao(connectionString)
{
    #region Gestión de Empresa y Seguridad

    public EmpresaDTO? ObtenerEmpresa(UsuarioDTO ejecutor)
    {
        ejecutor.ValidarAcceso("config");

        string sql = @"SELECT TOP 1 id, nombre_comercial, razon_social, cedula_juridica, 
                        tipo_regimen, telefono, correo_notificaciones, sitio_web, 
                        logo, permitir_stock_negativo FROM EMPRESA";

        DataTable dt = ExecuteQuery(sql, null, false);
        if (dt == null || dt.Rows.Count == 0) return null;

        DataRow row = dt.Rows[0];
        return new EmpresaDTO
        {
            Id = Convert.ToInt32(row["id"]),
            NombreComercial = row["nombre_comercial"]?.ToString() ?? string.Empty,
            RazonSocial = row["razon_social"]?.ToString() ?? string.Empty,
            CedulaJuridica = row["cedula_juridica"]?.ToString() ?? string.Empty,
            TipoRegimen = row["tipo_regimen"]?.ToString() ?? "Tradicional",
            Telefono = row["telefono"]?.ToString() ?? string.Empty,
            CorreoNotificaciones = row["correo_notificaciones"]?.ToString() ?? string.Empty,
            SitioWeb = row["sitio_web"]?.ToString() ?? string.Empty,
            Logo = row["logo"] != DBNull.Value ? row["logo"].ToString() : null,
            PermitirStockNegativo = row["permitir_stock_negativo"] != DBNull.Value && Convert.ToBoolean(row["permitir_stock_negativo"])
        };
    }

    public void GuardarEmpresa(EmpresaDTO empresa, UsuarioDTO ejecutor)
    {
        ejecutor.ValidarAcceso("config");
        ejecutor.ValidarEscritura();

        string sql = @"
            UPDATE EMPRESA SET 
                nombre_comercial = @nom, razon_social = @raz, cedula_juridica = @ced,
                tipo_regimen = @reg, telefono = @tel, correo_notificaciones = @cor, 
                sitio_web = @sit, permitir_stock_negativo = @stk 
            WHERE id = @id;
            
            IF @@ROWCOUNT = 0
            BEGIN
                INSERT INTO EMPRESA (nombre_comercial, razon_social, cedula_juridica, tipo_regimen, telefono, correo_notificaciones, sitio_web, permitir_stock_negativo)
                VALUES (@nom, @raz, @ced, @reg, @tel, @cor, @sit, @stk)
            END";

        var p = new SqlParameter[] {
            new SqlParameter("@id", empresa.Id),
            new SqlParameter("@nom", (object?)empresa.NombreComercial ?? DBNull.Value),
            new SqlParameter("@raz", (object?)empresa.RazonSocial ?? DBNull.Value),
            new SqlParameter("@ced", (object?)empresa.CedulaJuridica ?? DBNull.Value),
            new SqlParameter("@reg", (object?)empresa.TipoRegimen ?? DBNull.Value),
            new SqlParameter("@tel", (object?)empresa.Telefono ?? DBNull.Value),
            new SqlParameter("@cor", (object?)empresa.CorreoNotificaciones ?? DBNull.Value),
            new SqlParameter("@sit", (object?)empresa.SitioWeb ?? DBNull.Value),
            new SqlParameter("@stk", empresa.PermitirStockNegativo)
        };

        ExecuteNonQuery(sql, p, false);
    }

    #endregion

    #region Gestión de Licencias y Activación

    public bool ActualizarLicenciaExistente(string ruc, string hid, string llave, int maxEquipos)
    {
        string sql = @"
            DECLARE @EmpId INT;
            SELECT @EmpId = id FROM EMPRESA WHERE cedula_juridica = @ruc;
            IF @EmpId IS NOT NULL
            BEGIN
                IF EXISTS (SELECT 1 FROM SISTEMA_LICENCIA WHERE empresa_id = @EmpId)
                BEGIN
                    UPDATE SISTEMA_LICENCIA SET 
                        licencia_key = @llave, hardware_id = @hid, 
                        fecha_activacion = GETDATE(), fecha_vencimiento = DATEADD(YEAR, 1, GETDATE()),
                        estado = 'ACTIVO', limite_usuarios = @max 
                    WHERE empresa_id = @EmpId;
                END
                ELSE
                BEGIN
                    INSERT INTO SISTEMA_LICENCIA (empresa_id, licencia_key, hardware_id, fecha_activacion, fecha_vencimiento, estado, limite_usuarios)
                    VALUES (@EmpId, @llave, @hid, GETDATE(), DATEADD(YEAR, 1, GETDATE()), 'ACTIVO', @max);
                END
                SELECT 1;
            END
            ELSE SELECT 0;";

        var p = new SqlParameter[] {
            new SqlParameter("@ruc", ruc),
            new SqlParameter("@hid", hid),
            new SqlParameter("@llave", llave),
            new SqlParameter("@max", maxEquipos)
        };

        DataTable dt = ExecuteQuery(sql, p, false);
        return dt != null && dt.Rows.Count > 0 && Convert.ToInt32(dt.Rows[0][0]) > 0;
    }

    public DataTable ValidarLicenciaMultiEquipo(string hardwareId)
    {
        string sql = @"
            SELECT TOP 1 
                CASE WHEN L.estado = 'ACTIVO' THEN 'EQUIPO_AUTORIZADO' ELSE 'INACTIVO' END AS Resultado,
                E.cedula_juridica AS Ruc
            FROM SISTEMA_LICENCIA L
            JOIN EMPRESA E ON L.empresa_id = E.id
            WHERE L.hardware_id = @hid";

        var p = new SqlParameter[] { new SqlParameter("@hid", hardwareId) };
        return ExecuteQuery(sql, p, false);
    }

    #endregion

    #region Infraestructura Inicial (Métodos Requeridos por ConfiguracionService)

    /// <summary>
    /// Crea la sucursal por defecto si no existe.
    /// </summary>
    public void CrearSucursalBase(int empresaId)
    {
        string sql = @"IF NOT EXISTS (SELECT 1 FROM SUCURSAL WHERE id = 1) 
                       BEGIN
                           SET IDENTITY_INSERT SUCURSAL ON;
                           INSERT INTO SUCURSAL (id, nombre, activa, empresa_id) 
                           VALUES (1, 'Casa Matriz', 1, @empId);
                           SET IDENTITY_INSERT SUCURSAL OFF;
                       END";

        var p = new SqlParameter[] { new SqlParameter("@empId", empresaId) };
        ExecuteNonQuery(sql, p, false);
    }

    /// <summary>
    /// Registra la activación inicial a través de Store Procedure.
    /// </summary>
    public void RegistrarConfiguracionInicial(string nombreEmpresa, string ruc, int maxEquipos, string hid, UsuarioDTO ejecutor)
    {
        ejecutor.ValidarAcceso("config");
        ejecutor.ValidarEscritura();

        var parameters = new SqlParameter[] {
            new SqlParameter("@nombre_empresa", nombreEmpresa),
            new SqlParameter("@ruc", ruc),
            new SqlParameter("@max_equipos", maxEquipos),
            new SqlParameter("@hid_principal", hid)
        };

        ExecuteNonQuery("sp_Configuracion_ActivarSistema", parameters);
    }

    #endregion
}