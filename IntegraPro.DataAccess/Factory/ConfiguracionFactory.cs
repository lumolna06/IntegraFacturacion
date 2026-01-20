using IntegraPro.DataAccess.Dao;
using IntegraPro.DTO.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IntegraPro.DataAccess.Factory;

public class ConfiguracionFactory : MasterDao
{
    public ConfiguracionFactory(string connectionString) : base(connectionString) { }

    /// <summary>
    /// Registra la licencia inicial en la base de datos tras validar la llave de activación.
    /// </summary>
    public void RegistrarConfiguracionInicial(string empresa, string ruc, int max, string hid)
    {
        var parameters = new SqlParameter[]
        {
            new SqlParameter("@empresa", empresa),
            new SqlParameter("@ruc", ruc),
            new SqlParameter("@max", max),
            new SqlParameter("@hid", hid)
        };

        // Ejecutamos el procedimiento almacenado que limpia registros previos e inserta la nueva licencia
        ExecuteStoredProcedure("sp_Configuracion_ActivarSistema", parameters);
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