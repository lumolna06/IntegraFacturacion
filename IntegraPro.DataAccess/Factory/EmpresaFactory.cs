using IntegraPro.DataAccess.Dao;
using IntegraPro.DTO.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace IntegraPro.DataAccess.Factory;

public class EmpresaFactory(string connectionString) : MasterDao(connectionString)
{
    /// <summary>
    /// Obtiene la configuración global de la empresa. 
    /// Se añade el ejecutor para validar que tiene permiso de acceso al módulo.
    /// </summary>
    public EmpresaDTO? ObtenerConfiguracion(UsuarioDTO ejecutor)
    {
        // SEGURIDAD: Validar acceso preventivo
        ejecutor.ValidarAcceso("config");

        string sql = @"SELECT TOP 1 id, nombre_comercial, cedula_juridica, 
                                correo_notificaciones, tipo_regimen 
                       FROM EMPRESA";

        var dt = ExecuteQuery(sql, null, false);

        if (dt == null || dt.Rows.Count == 0) return null;

        DataRow r = dt.Rows[0];
        return new EmpresaDTO
        {
            Id = Convert.ToInt32(r["id"]),
            NombreComercial = r["nombre_comercial"]?.ToString() ?? "",
            CedulaJuridica = r["cedula_juridica"]?.ToString() ?? "",
            CorreoNotificaciones = r["correo_notificaciones"]?.ToString() ?? "",
            TipoRegimen = r["tipo_regimen"]?.ToString() ?? "Tradicional"
        };
    }

    /// <summary>
    /// Alias para compatibilidad con VentaService. 
    /// Nota: Si VentaService lo usa internamente para imprimir facturas, 
    /// asegúrate de pasar un ejecutor válido o sobrecargar si es un proceso automático.
    /// </summary>
    public EmpresaDTO? ObtenerEmpresa(UsuarioDTO ejecutor) => ObtenerConfiguracion(ejecutor);

    /// <summary>
    /// Registra la empresa por primera vez.
    /// </summary>
    public int RegistrarEmpresa(EmpresaDTO e, UsuarioDTO ejecutor)
    {
        // SEGURIDAD: Usamos los helpers estandarizados
        ejecutor.ValidarAcceso("config");
        ejecutor.ValidarEscritura();

        // 2. VALIDACIÓN DE EXISTENCIA PREVIA (Lógica de negocio intacta)
        string sqlCheck = "SELECT COUNT(*) FROM EMPRESA";
        var count = Convert.ToInt32(ExecuteScalar(sqlCheck, null, false));

        if (count > 0)
            throw new Exception("Ya existe una empresa registrada. Use Actualizar para modificar los datos.");

        string sql = @"INSERT INTO EMPRESA (nombre_comercial, cedula_juridica, correo_notificaciones, tipo_regimen)
                        VALUES (@nom, @ced, @cor, @reg);
                        SELECT CAST(SCOPE_IDENTITY() as int);";

        var p = new[] {
            new SqlParameter("@nom", e.NombreComercial),
            new SqlParameter("@ced", e.CedulaJuridica),
            new SqlParameter("@cor", (object?)e.CorreoNotificaciones ?? DBNull.Value),
            new SqlParameter("@reg", (object?)e.TipoRegimen ?? "Tradicional")
        };

        return Convert.ToInt32(ExecuteScalar(sql, p, false));
    }

    /// <summary>
    /// Actualiza los datos maestros.
    /// </summary>
    public void ActualizarEmpresa(EmpresaDTO e, UsuarioDTO ejecutor)
    {
        // SEGURIDAD: Usamos los helpers estandarizados
        ejecutor.ValidarAcceso("config");
        ejecutor.ValidarEscritura();

        string sql = @"UPDATE EMPRESA SET 
                        nombre_comercial = @nom, 
                        cedula_juridica = @ced,
                        correo_notificaciones = @cor, 
                        tipo_regimen = @reg
                       WHERE id = @id";

        var p = new[] {
            new SqlParameter("@id", e.Id),
            new SqlParameter("@nom", e.NombreComercial),
            new SqlParameter("@ced", e.CedulaJuridica),
            new SqlParameter("@cor", (object?)e.CorreoNotificaciones ?? DBNull.Value),
            new SqlParameter("@reg", (object?)e.TipoRegimen ?? "Tradicional")
        };

        ExecuteNonQuery(sql, p, false);
    }
}