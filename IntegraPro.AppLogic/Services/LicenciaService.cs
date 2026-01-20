using IntegraPro.AppLogic.Utils;
using IntegraPro.DataAccess.Factory;
using IntegraPro.DTO.Models;
using System.Management;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IntegraPro.AppLogic.Services;

public class LicenciaService
{
    private readonly ConfiguracionFactory _factory;

    public LicenciaService(ConfiguracionFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Obtiene un Identificador único de Hardware robusto.
    /// Si el serial de BIOS es genérico, busca en Placa Base o usa el nombre del equipo.
    /// </summary>
    public string GetHardwareId()
    {
        string serial = "";
        try
        {
            // 1. INTENTO: Serial de la Placa Base (Win32_BaseBoard)
            serial = GetWmiProperty("Win32_BaseBoard", "SerialNumber");

            // 2. INTENTO: Si el anterior es inválido, Serial de la BIOS
            if (EsInvalido(serial))
            {
                serial = GetWmiProperty("Win32_BIOS", "SerialNumber");
            }

            // 3. INTENTO: Si sigue siendo inválido, ID del Procesador
            if (EsInvalido(serial))
            {
                serial = GetWmiProperty("Win32_Processor", "ProcessorId");
            }

            // 4. RECURSO FINAL: Nombre de la PC (Garantiza que no sea "Default string")
            if (EsInvalido(serial))
            {
                serial = $"PC-{Environment.MachineName.ToUpper()}";
            }
        }
        catch
        {
            serial = $"FALLBACK-{Environment.MachineName.ToUpper()}";
        }

        return serial;
    }

    /// <summary>
    /// Valida si la PC actual está autorizada en la base de datos de configuraciones.
    /// </summary>
    public ApiResponse<bool> ValidarSistema()
    {
        try
        {
            string hid = GetHardwareId();

            // Ejecutamos el SP multinivel que creamos en SQL a través de la Factory
            DataTable dt = _factory.ValidarLicenciaMultiEquipo(hid);

            if (dt != null && dt.Rows.Count > 0)
            {
                string resultado = dt.Rows[0]["Resultado"]?.ToString() ?? "";

                // Verificamos las respuestas definidas en tu Stored Procedure
                if (resultado == "EQUIPO_AUTORIZADO" || resultado == "NUEVO_EQUIPO_REGISTRADO")
                {
                    return new ApiResponse<bool>(true, $"Acceso concedido: {resultado}", true);
                }

                if (resultado == "LIMITE_ALCANZADO")
                {
                    return new ApiResponse<bool>(false, "Límite de licencias alcanzado para este paquete.");
                }
            }

            return new ApiResponse<bool>(false, "Licencia no válida o equipo no autorizado.");
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool>(false, $"Error crítico en validación: {ex.Message}");
        }
    }

    // --- MÉTODOS PRIVADOS DE APOYO ---

    private string GetWmiProperty(string table, string property)
    {
        try
        {
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher($"SELECT {property} FROM {table}"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    return obj[property]?.ToString()?.Trim() ?? "";
                }
            }
        }
        catch { }
        return "";
    }

    private bool EsInvalido(string sid)
    {
        if (string.IsNullOrWhiteSpace(sid)) return true;

        string val = sid.ToLower();
        // Lista de valores genéricos que Windows devuelve cuando no hay serial real
        return val == "default string" ||
               val == "none" ||
               val == "to be filled by o.e.m." ||
               val == "00000000" ||
               val == "unknown";
    }
}