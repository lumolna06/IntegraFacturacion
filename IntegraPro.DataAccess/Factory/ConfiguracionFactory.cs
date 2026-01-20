using IntegraPro.DataAccess.Dao;
using IntegraPro.DTO.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IntegraPro.DataAccess.Factory;

public class ConfiguracionFactory : MasterDao
{
    public ConfiguracionFactory(string connectionString) : base(connectionString) { }

    public void GuardarEmpresa(EmpresaDTO empresa)
    {
        var p = new SqlParameter[] {
            new SqlParameter("@nombre_comercial", empresa.NombreComercial),
            new SqlParameter("@razon_social", empresa.NombreComercial), // Simplificado
            new SqlParameter("@cedula_juridica", empresa.CedulaJuridica),
            new SqlParameter("@tipo_regimen", empresa.TipoRegimen),
            new SqlParameter("@telefono", "00000000"),
            new SqlParameter("@correo_notificaciones", empresa.CorreoNotificaciones),
            new SqlParameter("@sitio_web", "")
        };
        ExecuteNonQuery("sp_Empresa_Upsert", p);
    }

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

    // En IntegraPro.DataAccess/Factory/ConfiguracionFactory.cs

    public DataTable ValidarLicenciaMultiEquipo(string hardwareId)
    {
        var parameters = new Microsoft.Data.SqlClient.SqlParameter[]
        {
        new Microsoft.Data.SqlClient.SqlParameter("@hardware_id", hardwareId)
        };

        // Aquí sí puedes usar ExecuteQuery porque ConfiguracionFactory HEREDA de MasterDao
        return ExecuteQuery("sp_Licencia_AutoValidar_MultiEquipo", parameters);
    }
}