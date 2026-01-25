using IntegraPro.DataAccess.Dao;
using IntegraPro.DTO.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace IntegraPro.DataAccess.Factory;

public class ConfiguracionFactory(string connectionString) : MasterDao(connectionString)
{
    /// <summary>
    /// Obtiene los datos comerciales. Ahora requiere validación de acceso al módulo config.
    /// </summary>
    public EmpresaDTO? ObtenerEmpresa(UsuarioDTO ejecutor)
    {
        // SEGURIDAD: Validar que el usuario tiene permiso para ver la configuración
        ejecutor.ValidarAcceso("config");

        string sql = @"SELECT TOP 1 
                        id, nombre_comercial, razon_social, cedula_juridica, 
                        tipo_regimen, telefono, correo_notificaciones, sitio_web, 
                        logo, permitir_stock_negativo
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
    /// Guarda o actualiza con lógica blindada.
    /// </summary>
    public void GuardarEmpresa(EmpresaDTO empresa, UsuarioDTO ejecutor)
    {
        // SEGURIDAD: Reemplazamos el 'if' manual por los helpers estandarizados
        ejecutor.ValidarAcceso("config");
        ejecutor.ValidarEscritura();

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
    /// Registra la licencia. Mantenemos el permiso 'config'.
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

    // Los métodos ValidarLicenciaMultiEquipo y ObtenerLicencia se quedan igual
    // ya que se usan en procesos de arranque o validación de equipo físico (pre-auth).

    public DataTable ValidarLicenciaMultiEquipo(string hardwareId)
    {
        var parameters = new SqlParameter[] { new SqlParameter("@hardware_id", hardwareId) };
        return ExecuteQuery("sp_Licencia_AutoValidar_MultiEquipo", parameters);
    }

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