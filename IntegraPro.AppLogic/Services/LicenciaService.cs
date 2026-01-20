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
    private readonly string _masterKey = "IntegraPro_Secret_2026";
    // URL por defecto para el control de bloqueos
    private readonly string _killSwitchUrl = "https://tu-servidor-remoto.com/blacklist.txt";

    public LicenciaService(ConfiguracionFactory factory)
    {
        _factory = factory;
    }

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

    public string GenerarLlave(string ruc, string hid, int maxEquipos)
    {
        using (var sha = SHA256.Create())
        {
            string rawData = $"{ruc.Trim()}-{hid.Trim()}-{maxEquipos}-{_masterKey}";
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            return Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Substring(0, 16).ToUpper();
        }
    }

    public string GetHardwareId()
    {
        string serial = "";
        try
        {
            serial = GetWmiProperty("Win32_BaseBoard", "SerialNumber");
            if (EsInvalido(serial)) serial = GetWmiProperty("Win32_BIOS", "SerialNumber");
            if (EsInvalido(serial)) serial = GetWmiProperty("Win32_Processor", "ProcessorId");
            if (EsInvalido(serial)) serial = $"PC-{Environment.MachineName.ToUpper()}";
        }
        catch
        {
            serial = $"FALLBACK-{Environment.MachineName.ToUpper()}";
        }
        return serial.Trim();
    }

    /// <summary>
    /// Valida la licencia local y verifica el Kill Switch remoto.
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
                    // --- VALIDACIÓN DE KILL SWITCH REMOTO ---
                    // Si el RUC está en la lista negra, bloqueamos aunque la licencia local sea válida
                    if (!string.IsNullOrEmpty(rucRegistrado))
                    {
                        var bloqueoRemoto = ValidarKillSwitchRemoto(rucRegistrado);
                        if (bloqueoRemoto)
                        {
                            return new ApiResponse<bool>(false, "Esta licencia ha sido revocada remotamente por el administrador.");
                        }
                    }

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

    /// <summary>
    /// Consulta una URL externa para verificar si el RUC del cliente está bloqueado.
    /// </summary>
    private bool ValidarKillSwitchRemoto(string ruc)
    {
        try
        {
            using (var client = new HttpClient())
            {
                // Timeout corto para no afectar la experiencia del usuario si no hay internet
                client.Timeout = TimeSpan.FromSeconds(2);
                var response = client.GetStringAsync(_killSwitchUrl).Result;

                // Si la respuesta contiene el RUC, es que está bloqueado
                return response.Contains(ruc);
            }
        }
        catch
        {
            // Si falla la conexión (ej. no hay internet), permitimos el acceso
            // ya que la licencia local ya fue validada previamente.
            return false;
        }
    }

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
        return val == "default string" || val == "none" || val == "to be filled by o.e.m." ||
               val == "00000000" || val == "unknown";
    }
}