using IntegraPro.DataAccess.Dao;
using IntegraPro.DTO.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace IntegraPro.DataAccess.Factory;

public class ConfiguracionFactory : MasterDao
{
    public ConfiguracionFactory(string connectionString) : base(connectionString) { }

    /// <summary>
    /// Obtiene todos los datos comerciales de la empresa de la tabla EMPRESA.
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
                        logo 
                       FROM EMPRESA";

        DataTable dt = ExecuteQuery(sql, null, false);

        if (dt.Rows.Count == 0) return null;

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
            Logo = row["logo"]?.ToString()
        };
    }

    /// <summary>
    /// Guarda o actualiza los datos comerciales de la empresa con validación anti-basura.
    /// </summary>
    public void GuardarEmpresa(EmpresaDTO empresa)
    {
        // Lógica de protección: Solo actualiza si el valor no es nulo, no está vacío y no es "string"
        string sql = @"
            UPDATE EMPRESA SET 
                nombre_comercial = CASE WHEN ISNULL(@nom, '') NOT IN ('', 'string') THEN @nom ELSE nombre_comercial END,
                razon_social = CASE WHEN ISNULL(@raz, '') NOT IN ('', 'string') THEN @raz ELSE razon_social END,
                cedula_juridica = CASE WHEN ISNULL(@ced, '') NOT IN ('', 'string') THEN @ced ELSE cedula_juridica END,
                tipo_regimen = CASE WHEN ISNULL(@reg, '') NOT IN ('', 'string') THEN @reg ELSE tipo_regimen END,
                telefono = CASE WHEN ISNULL(@tel, '') NOT IN ('', 'string') THEN @tel ELSE telefono END,
                correo_notificaciones = CASE WHEN ISNULL(@cor, '') NOT IN ('', 'string') THEN @cor ELSE correo_notificaciones END,
                sitio_web = CASE WHEN ISNULL(@sit, '') NOT IN ('', 'string') THEN @sit ELSE sitio_web END
            WHERE id = 1;
            
            -- Si no existe el registro 1, lo insertamos (Upsert manual de seguridad)
            IF @@ROWCOUNT = 0
            BEGIN
                INSERT INTO EMPRESA (nombre_comercial, razon_social, cedula_juridica, tipo_regimen, telefono, correo_notificaciones, sitio_web)
                VALUES (@nom, @raz, @ced, @reg, @tel, @cor, @sit)
            END";

        var p = new SqlParameter[] {
            new SqlParameter("@nom", (object)empresa.NombreComercial ?? DBNull.Value),
            new SqlParameter("@raz", (object)empresa.RazonSocial ?? DBNull.Value),
            new SqlParameter("@ced", (object)empresa.CedulaJuridica ?? DBNull.Value),
            new SqlParameter("@reg", (object)empresa.TipoRegimen ?? DBNull.Value),
            new SqlParameter("@tel", (object)empresa.Telefono ?? DBNull.Value),
            new SqlParameter("@cor", (object)empresa.CorreoNotificaciones ?? DBNull.Value),
            new SqlParameter("@sit", (object)empresa.SitioWeb ?? DBNull.Value)
        };

        ExecuteNonQuery(sql, p, false);
    }

    /// <summary>
    /// Registra la licencia inicial en la base de datos.
    /// </summary>
    public void RegistrarConfiguracionInicial(string nombreEmpresa, string ruc, int maxEquipos, string hid)
    {
        var parameters = new SqlParameter[] {
            new SqlParameter("@nombre_empresa", nombreEmpresa),
            new SqlParameter("@ruc", ruc),
            new SqlParameter("@max_equipos", maxEquipos),
            new SqlParameter("@hid_principal", hid)
        };

        ExecuteNonQuery("sp_Configuracion_ActivarSistema", parameters);
    }

    /// <summary>
    /// Valida el hardware ID contra la licencia.
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

        if (dt.Rows.Count == 0) return null;

        var row = dt.Rows[0];
        return new LicenciaDTO
        {
            HardwareId = row["hardware_id"].ToString()!,
            Estado = row["estado"].ToString()!,
            FechaVencimiento = Convert.ToDateTime(row["fecha_vencimiento"])
        };
    }
}