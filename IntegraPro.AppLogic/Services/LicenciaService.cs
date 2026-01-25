using IntegraPro.AppLogic.Utils;
using IntegraPro.DataAccess.Factory;
using IntegraPro.DTO.Models;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Data;

namespace IntegraPro.AppLogic.Services;

public class LicenciaService
{
    private readonly ConfiguracionFactory _factory;
    private readonly string _masterKey = "IntegraPro_Secret_2026";
    private readonly string _killSwitchUrl = "https://tu-servidor-remoto.com/blacklist.txt";

    public LicenciaService(ConfiguracionFactory factory)
    {
        _factory = factory;
    }

    public ApiResponse<bool> ActivarLicencia(string llaveActivacion, string nombreEmpresa, string ruc, int maxEquipos, UsuarioDTO ejecutor)
    {
        try
        {
            string hidActual = GetHardwareId();
            string llaveEsperada = GenerarLlave(ruc, hidActual, maxEquipos);

            if (llaveActivacion != llaveEsperada)
                return new ApiResponse<bool>(false, "La llave de activación es inválida.");

            _factory.RegistrarConfiguracionInicial(nombreEmpresa, ruc, maxEquipos, hidActual, ejecutor);
            return new ApiResponse<bool>(true, "¡Sistema activado con éxito!", true);
        }
        catch (UnauthorizedAccessException ex) { return new ApiResponse<bool>(false, ex.Message); }
        catch (Exception ex) { return new ApiResponse<bool>(false, $"Error: {ex.Message}"); }
    }

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
                    if (!string.IsNullOrEmpty(rucRegistrado) && EstaEnListaNegra(rucRegistrado))
                        return new ApiResponse<bool>(false, "Licencia revocada.");

                    return new ApiResponse<bool>(true, "Acceso concedido", true);
                }
                if (resultado == "LIMITE_ALCANZADO") return new ApiResponse<bool>(false, "Límite de equipos alcanzado.");
            }
            return new ApiResponse<bool>(false, "Sistema no activado.");
        }
        catch (Exception ex) { return new ApiResponse<bool>(false, $"Error: {ex.Message}"); }
    }

    public string GenerarLlave(string ruc, string hid, int maxEquipos)
    {
        using var sha = SHA256.Create();
        string rawData = $"{ruc.Trim()}-{hid.Trim()}-{maxEquipos}-{_masterKey}";
        byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        return Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "").Substring(0, 16).ToUpper();
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
        catch { serial = $"FB-{Environment.MachineName.ToUpper()}"; }
        return serial.Trim();
    }

    private bool EstaEnListaNegra(string ruc)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            var response = client.GetStringAsync(_killSwitchUrl).Result;
            return response.Contains(ruc);
        }
        catch { return false; }
    }

    private string GetWmiProperty(string table, string property)
    {
        try
        {
            using ManagementObjectSearcher searcher = new ManagementObjectSearcher($"SELECT {property} FROM {table}");
            foreach (ManagementObject obj in searcher.Get()) return obj[property]?.ToString()?.Trim() ?? "";
        }
        catch { }
        return "";
    }

    private bool EsInvalido(string sid)
    {
        if (string.IsNullOrWhiteSpace(sid)) return true;
        string val = sid.ToLower();
        return val == "default string" || val == "none" || val == "00000000" || val.Length < 5;
    }
}