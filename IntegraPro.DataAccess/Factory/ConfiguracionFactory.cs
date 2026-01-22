using IntegraPro.DataAccess.Dao;
using IntegraPro.DTO.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace IntegraPro.DataAccess.Factory;

public class ConfiguracionFactory : MasterDao
{
    public ConfiguracionFactory(string connectionString) : base(connectionString) { }

    // ==========================================
    // MÉTODO AGREGADO: LECTURA DE DATOS DE EMPRESA
    // ==========================================
    /// <summary>
    /// Obtiene los datos comerciales de la empresa para los encabezados de documentos.
    /// </summary>
    public EmpresaDTO? ObtenerEmpresa()
    {
        string sql = "SELECT TOP 1 id, nombre_comercial, cedula_juridica, correo_notificaciones, tipo_regimen FROM EMPRESA";

        DataTable dt = ExecuteQuery(sql, null, false);

        if (dt.Rows.Count == 0) return null;

        DataRow row = dt.Rows[0];
        return new EmpresaDTO
        {
            Id = Convert.ToInt32(row["id"]),
            NombreComercial = row["nombre_comercial"]?.ToString() ?? string.Empty,
            CedulaJuridica = row["cedula_juridica"]?.ToString() ?? string.Empty,
            CorreoNotificaciones = row["correo_notificaciones"]?.ToString() ?? string.Empty,
            TipoRegimen = row["tipo_regimen"]?.ToString() ?? "Tradicional"
        };
    }

    /// <summary>
    /// Registra la licencia inicial en la base de datos tras validar la llave de activación.
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
    /// Llama al SP multinivel para validar si el equipo actual tiene acceso o si se debe auto-registrar.
    /// </summary>
    public DataTable ValidarLicenciaMultiEquipo(string hardwareId)
    {
        var parameters = new SqlParameter[]
        {
            new SqlParameter("@hardware_id", hardwareId)
        };

        return ExecuteQuery("sp_Licencia_AutoValidar_MultiEquipo", parameters);
    }

    /// <summary>
    /// Guarda los datos comerciales de la empresa.
    /// </summary>
    public void GuardarEmpresa(EmpresaDTO empresa)
    {
        var p = new SqlParameter[] {
            new SqlParameter("@nombre_comercial", empresa.NombreComercial),
            new SqlParameter("@razon_social", empresa.NombreComercial),
            new SqlParameter("@cedula_juridica", empresa.CedulaJuridica),
            new SqlParameter("@tipo_regimen", empresa.TipoRegimen),
            new SqlParameter("@telefono", "00000000"),
            new SqlParameter("@correo_notificaciones", empresa.CorreoNotificaciones),
            new SqlParameter("@sitio_web", "")
        };

        ExecuteNonQuery("sp_Empresa_Upsert", p);
    }

    /// <summary>
    /// Método de legado para obtener licencia simple.
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