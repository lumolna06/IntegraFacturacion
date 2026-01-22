using IntegraPro.DataAccess.Dao;
using IntegraPro.DTO.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace IntegraPro.DataAccess.Factory;

public class EmpresaFactory(string connectionString) : MasterDao(connectionString)
{
    public EmpresaDTO ObtenerConfiguracion()
    {
        string sql = @"SELECT TOP 1 id, nombre_comercial, cedula_juridica, 
                              correo_notificaciones, tipo_regimen 
                       FROM EMPRESA";

        var dt = ExecuteQuery(sql, null, false);

        if (dt.Rows.Count == 0) return null;

        DataRow r = dt.Rows[0];
        return new EmpresaDTO
        {
            Id = Convert.ToInt32(r["id"]),
            NombreComercial = r["nombre_comercial"].ToString() ?? "",
            CedulaJuridica = r["cedula_juridica"].ToString() ?? "",
            CorreoNotificaciones = r["correo_notificaciones"].ToString() ?? "",
            TipoRegimen = r["tipo_regimen"]?.ToString() ?? "Tradicional"
        };
    }

    public int RegistrarEmpresa(EmpresaDTO e)
    {
        string sqlCheck = "SELECT COUNT(*) FROM EMPRESA";
        var count = (int)ExecuteScalar(sqlCheck, null, false);

        if (count > 0)
            throw new Exception("Ya existe una empresa registrada.");

        string sql = @"INSERT INTO EMPRESA (nombre_comercial, cedula_juridica, correo_notificaciones, tipo_regimen)
                        VALUES (@nom, @ced, @cor, @reg);
                        SELECT SCOPE_IDENTITY();";

        var p = new[] {
            new SqlParameter("@nom", e.NombreComercial),
            new SqlParameter("@ced", e.CedulaJuridica),
            new SqlParameter("@cor", e.CorreoNotificaciones),
            new SqlParameter("@reg", e.TipoRegimen)
        };

        object result = ExecuteScalar(sql, p, false);
        return Convert.ToInt32(result);
    }

    public void ActualizarEmpresa(EmpresaDTO e)
    {
        string sql = @"UPDATE EMPRESA SET 
                        nombre_comercial = @nom, cedula_juridica = @ced,
                        correo_notificaciones = @cor, tipo_regimen = @reg
                       WHERE id = @id";

        var p = new[] {
            new SqlParameter("@id", e.Id),
            new SqlParameter("@nom", e.NombreComercial),
            new SqlParameter("@ced", e.CedulaJuridica),
            new SqlParameter("@cor", e.CorreoNotificaciones),
            new SqlParameter("@reg", e.TipoRegimen)
        };

        ExecuteNonQuery(sql, p, false);
    }
}