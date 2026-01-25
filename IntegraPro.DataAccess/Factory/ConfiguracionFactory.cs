using IntegraPro.DataAccess.Dao;
using IntegraPro.DTO.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace IntegraPro.DataAccess.Factory;

public class ConfiguracionFactory(string connectionString) : MasterDao(connectionString)
{
    /// <summary>
    /// Obtiene todos los datos comerciales de la empresa e indicadores de configuración. 
    /// </summary>
    public EmpresaDTO? ObtenerEmpresa()
    {
        string sql = @"SELECT TOP 1 
                        id, 
                        nombre_comercial, 
                        razon_social, 
                        cedula_juridica, 
                        tipo_regimen, 
                        telefono, 
                        correo_notificaciones, 
                        sitio_web, 
                        logo,
                        permitir_stock_negativo
                       FROM EMPRESA";

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
            Logo = row["logo"]?.ToString(),
            PermitirStockNegativo = row["permitir_stock_negativo"] != DBNull.Value && Convert.ToBoolean(row["permitir_stock_negativo"])
        };
    }

    /// <summary>
    /// Guarda o actualiza con lógica anti-basura. Solo permitido para roles con permiso 'config'.
    /// </summary>
    public void GuardarEmpresa(EmpresaDTO empresa, UsuarioDTO ejecutor)
    {
        // 1. VALIDACIÓN DE SEGURIDAD
        if (!ejecutor.TienePermiso("config") || ejecutor.TienePermiso("solo_lectura"))
            throw new UnauthorizedAccessException("No tiene permisos para modificar la configuración global del sistema.");

        // 2. Lógica de protección: Solo actualiza si el valor no es nulo/vacío para textos, y actualiza el bit de stock.
        string sql = @"
            UPDATE EMPRESA SET 
                nombre_comercial = CASE WHEN ISNULL(@nom, '') NOT IN ('', 'string') THEN @nom ELSE nombre_comercial END,
                razon_social = CASE WHEN ISNULL(@raz, '') NOT IN ('', 'string') THEN @raz ELSE razon_social END,
                cedula_juridica = CASE WHEN ISNULL(@ced, '') NOT IN ('', 'string') THEN @ced ELSE cedula_juridica END,
                tipo_regimen = CASE WHEN ISNULL(@reg, '') NOT IN ('', 'string') THEN @reg ELSE tipo_regimen END,
                telefono = CASE WHEN ISNULL(@tel, '') NOT IN ('', 'string') THEN @tel ELSE telefono END,
                correo_notificaciones = CASE WHEN ISNULL(@cor, '') NOT IN ('', 'string') THEN @cor ELSE correo_notificaciones END,
                sitio_web = CASE WHEN ISNULL(@sit, '') NOT IN ('', 'string') THEN @sit ELSE sitio_web END,
                permitir_stock_negativo = @stk
            WHERE id = 1;
            
            IF @@ROWCOUNT = 0
            BEGIN
                INSERT INTO EMPRESA (nombre_comercial, razon_social, cedula_juridica, tipo_regimen, telefono, correo_notificaciones, sitio_web, permitir_stock_negativo)
                VALUES (@nom, @raz, @ced, @reg, @tel, @cor, @sit, @stk)
            END";

        var p = new SqlParameter[] {
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

    /// <summary>
    /// Registra la licencia inicial. Requiere permisos de configuración.
    /// </summary>
    public void RegistrarConfiguracionInicial(string nombreEmpresa, string ruc, int maxEquipos, string hid, UsuarioDTO ejecutor)
    {
        if (!ejecutor.TienePermiso("config"))
            throw new UnauthorizedAccessException("Acción denegada para el registro de licencia.");

        var parameters = new SqlParameter[] {
            new SqlParameter("@nombre_empresa", nombreEmpresa),
            new SqlParameter("@ruc", ruc),
            new SqlParameter("@max_equipos", maxEquipos),
            new SqlParameter("@hid_principal", hid)
        };

        ExecuteNonQuery("sp_Configuracion_ActivarSistema", parameters);
    }

    /// <summary>
    /// Valida el hardware ID contra la licencia. No requiere 'ejecutor' ya que ocurre antes del login.
    /// </summary>
    public DataTable ValidarLicenciaMultiEquipo(string hardwareId)
    {
        var parameters = new SqlParameter[] { new SqlParameter("@hardware_id", hardwareId) };
        return ExecuteQuery("sp_Licencia_AutoValidar_MultiEquipo", parameters);
    }

    /// <summary>
    /// Obtiene información de licencia.
    /// </summary>
    public LicenciaDTO? ObtenerLicencia(string hardwareId)
    {
        var p = new SqlParameter[] { new SqlParameter("@hardware_id", hardwareId) };
        var dt = ExecuteQuery("sp_Licencia_Validar", p);

        if (dt == null || dt.Rows.Count == 0) return null;

        var row = dt.Rows[0];
        return new LicenciaDTO
        {
            HardwareId = row["hardware_id"].ToString()!,
            Estado = row["estado"].ToString()!,
            FechaVencimiento = Convert.ToDateTime(row["fecha_vencimiento"])
        };
    }
}