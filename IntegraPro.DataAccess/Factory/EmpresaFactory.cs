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
    /// </summary>
    public EmpresaDTO? ObtenerEmpresa(UsuarioDTO ejecutor) => ObtenerConfiguracion(ejecutor);

    /// <summary>
    /// Registra la empresa por primera vez (Modo Instalación compatible).
    /// </summary>
    public int RegistrarEmpresa(EmpresaDTO e, UsuarioDTO? ejecutor)
    {
        // 1. VALIDACIÓN DE EXISTENCIA PREVIA
        string sqlCheck = "SELECT COUNT(*) FROM EMPRESA";
        var count = Convert.ToInt32(ExecuteScalar(sqlCheck, null, false));

        // 2. LÓGICA DE SEGURIDAD (Solo aplica si ya existe una configuración)
        if (count > 0)
        {
            if (ejecutor == null) throw new Exception("Acceso denegado: El sistema ya está configurado.");

            ejecutor.ValidarAcceso("config");
            ejecutor.ValidarEscritura();

            throw new Exception("Ya existe una empresa registrada. Use Actualizar para modificar los datos.");
        }

        // 3. INSERCIÓN (Se incluyen campos NOT NULL obligatorios según script SQL)
        string sql = @"INSERT INTO EMPRESA (nombre_comercial, razon_social, cedula_juridica, correo_notificaciones, tipo_regimen, permitir_stock_negativo)
                        VALUES (@nom, @raz, @ced, @cor, @reg, @stk);
                        SELECT CAST(SCOPE_IDENTITY() as int);";

        var p = new[] {
            new SqlParameter("@nom", e.NombreComercial),
            new SqlParameter("@raz", string.IsNullOrEmpty(e.RazonSocial) ? e.NombreComercial : e.RazonSocial),
            new SqlParameter("@ced", e.CedulaJuridica),
            new SqlParameter("@cor", (object?)e.CorreoNotificaciones ?? DBNull.Value),
            new SqlParameter("@reg", (object?)e.TipoRegimen ?? "Tradicional"),
            new SqlParameter("@stk", e.PermitirStockNegativo)
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
                        razon_social = @raz,
                        cedula_juridica = @ced,
                        correo_notificaciones = @cor, 
                        tipo_regimen = @reg,
                        permitir_stock_negativo = @stk
                       WHERE id = @id";

        var p = new[] {
            new SqlParameter("@id", e.Id),
            new SqlParameter("@nom", e.NombreComercial),
            new SqlParameter("@raz", e.RazonSocial),
            new SqlParameter("@ced", e.CedulaJuridica),
            new SqlParameter("@cor", (object?)e.CorreoNotificaciones ?? DBNull.Value),
            new SqlParameter("@reg", (object?)e.TipoRegimen ?? "Tradicional"),
            new SqlParameter("@stk", e.PermitirStockNegativo)
        };

        ExecuteNonQuery(sql, p, false);
    }
}