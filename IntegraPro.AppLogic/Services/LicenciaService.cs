using IntegraPro.AppLogic.Utils;
using IntegraPro.DataAccess.Factory;
using IntegraPro.DTO.Models;
using System.Management;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace IntegraPro.AppLogic.Services;

public class LicenciaService
{
    private readonly ConfiguracionFactory _factory;

    // Esta llave debe ser la misma en tu generador de licencias privado
    private readonly string _masterKey = "IntegraPro_Secret_2026";

    // URL para bloquear clientes morosos o licencias robadas (formato: lista de RUCs separados por comas o lineas)
    private readonly string _killSwitchUrl = "https://tu-servidor-remoto.com/blacklist.txt";

    public LicenciaService(ConfiguracionFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Activa el sistema por primera vez vinculándolo al Hardware ID principal.
    /// </summary>
    public ApiResponse<bool> ActivarLicencia(string llaveActivacion, string nombreEmpresa, string ruc, int maxEquipos)
    {
        try
        {
            string hidActual = GetHardwareId();
            string llaveEsperada = GenerarLlave(ruc, hidActual, maxEquipos);

            if (llaveActivacion != llaveEsperada)
            {
                return new ApiResponse<bool>(false, "La llave de activación es inválida para este equipo o los datos no coinciden.");
            }

            _factory.RegistrarConfiguracionInicial(nombreEmpresa, ruc, maxEquipos, hidActual);

            return new ApiResponse<bool>(true, "¡Sistema activado con éxito!", true);
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool>(false, $"Error al activar: {ex.Message}");
        }
    }

    /// <summary>
    /// Algoritmo de generación de llaves (debe ser idéntico en tu generador externo)
    /// </summary>
    public string GenerarLlave(string ruc, string hid, int maxEquipos)
    {
        using (var sha = SHA256.Create())
        {
            // El formato es RUC-HID-MAX-KEY
            string rawData = $"{ruc.Trim()}-{hid.Trim()}-{maxEquipos}-{_masterKey}";
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(rawData));

            // Retornamos un código corto de 16 caracteres para facilidad del usuario
            return Convert.ToBase64String(bytes)
                .Replace("+", "")
                .Replace("/", "")
                .Replace("=", "")
                .Substring(0, 16)
                .ToUpper();
        }
    }

    /// <summary>
    /// Valida si el equipo actual tiene permiso de ejecución.
    /// </summary>
    public ApiResponse<bool> ValidarSistema()
    {
        try
        {
            string hid = GetHardwareId();
            DataTable dt = _factory.ValidarLicenciaMultiEquipo(hid);

            if (dt != null && dt.Rows.Count > 0)
            {
                string resultado = dt.Rows[0]["Resultado"]?.ToString() ?? "";
                string rucRegistrado = dt.Rows[0]["Ruc"]?.ToString() ?? "";

                if (resultado == "EQUIPO_AUTORIZADO" || resultado == "NUEVO_EQUIPO_REGISTRADO")
                {
                    // Validación de seguridad adicional (Remota)
                    if (!string.IsNullOrEmpty(rucRegistrado))
                    {
                        if (EstaEnListaNegra(rucRegistrado))
                        {
                            return new ApiResponse<bool>(false, "Licencia revocada: Por favor contacte al soporte técnico.");
                        }
                    }

                    return new ApiResponse<bool>(true, $"Acceso concedido", true);
                }

                if (resultado == "LIMITE_ALCANZADO")
                {
                    return new ApiResponse<bool>(false, "Se ha alcanzado el límite de equipos permitidos para esta licencia.");
                }
            }

            return new ApiResponse<bool>(false, "Sistema no activado o hardware no reconocido.");
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool>(false, $"Error crítico de seguridad: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtiene el Identificador único de Hardware (HID)
    /// </summary>
    public string GetHardwareId()
    {
        string serial = "";
        try
        {
            // Intentar con Serial de la Placa Base (Motherboard)
            serial = GetWmiProperty("Win32_BaseBoard", "SerialNumber");

            // Fallback 1: BIOS
            if (EsInvalido(serial)) serial = GetWmiProperty("Win32_BIOS", "SerialNumber");

            // Fallback 2: ID del Procesador
            if (EsInvalido(serial)) serial = GetWmiProperty("Win32_Processor", "ProcessorId");

            // Fallback Final: Nombre de la máquina (Menos seguro pero funcional)
            if (EsInvalido(serial)) serial = $"PC-{Environment.MachineName.ToUpper()}";
        }
        catch
        {
            serial = $"FB-{Environment.MachineName.ToUpper()}";
        }
        return serial.Trim();
    }

    private bool EstaEnListaNegra(string ruc)
    {
        try
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(2); // No bloqueamos al usuario si no hay internet
                var response = client.GetStringAsync(_killSwitchUrl).Result;
                return response.Contains(ruc);
            }
        }
        catch
        {
            return false; // Si falla el internet, permitimos seguir trabajando localmente
        }
    }

    private string GetWmiProperty(string table, string property)
    {
        try
        {
            // Nota: Requiere NuGet System.Management
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
        return val == "default string" || val == "none" || val == "to be filled by o.e.m." ||
               val == "00000000" || val == "unknown" || val.Length < 5;
    }
}