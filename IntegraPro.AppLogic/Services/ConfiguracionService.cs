using IntegraPro.AppLogic.Interfaces;
using IntegraPro.AppLogic.Utils;
using IntegraPro.DataAccess.Factory;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Services;

public class ConfiguracionService(ConfiguracionFactory factory) : IConfiguracionService
{
    private readonly ConfiguracionFactory _factory = factory;

    /// <summary>
    /// Recupera los datos de la empresa. Ahora requiere el ejecutor para validar acceso.
    /// </summary>
    public ApiResponse<EmpresaDTO> ObtenerDatosEmpresa(UsuarioDTO ejecutor)
    {
        try
        {
            // --- SEGURIDAD: Validación preventiva ---
            ejecutor.ValidarAcceso("config");

            var datos = _factory.ObtenerEmpresa(ejecutor);
            if (datos == null)
                return new ApiResponse<EmpresaDTO>(false, "No se encontraron datos de configuración de la empresa.");

            return new ApiResponse<EmpresaDTO>(true, "Datos recuperados exitosamente.", datos);
        }
        catch (UnauthorizedAccessException ex)
        {
            return new ApiResponse<EmpresaDTO>(false, ex.Message);
        }
        catch (Exception ex)
        {
            return new ApiResponse<EmpresaDTO>(false, $"Error al obtener datos: {ex.Message}");
        }
    }

    /// <summary>
    /// Actualiza la información comercial.
    /// </summary>
    public ApiResponse<bool> ActualizarEmpresa(EmpresaDTO empresa, UsuarioDTO ejecutor)
    {
        try
        {
            // --- SEGURIDAD: Validación preventiva ---
            ejecutor.ValidarAcceso("config");
            ejecutor.ValidarEscritura();

            _factory.GuardarEmpresa(empresa, ejecutor);
            return new ApiResponse<bool>(true, "Empresa actualizada correctamente.", true);
        }
        catch (UnauthorizedAccessException ex)
        {
            return new ApiResponse<bool>(false, ex.Message, false);
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool>(false, $"Error crítico: {ex.Message}", false);
        }
    }

    /// <summary>
    /// Realiza el registro inicial en la base de datos.
    /// </summary>
    public ApiResponse<bool> RegistrarLicenciaInicial(string nombre, string ruc, int equipos, string hid, UsuarioDTO ejecutor)
    {
        try
        {
            // --- SEGURIDAD: Validación preventiva ---
            ejecutor.ValidarAcceso("config");
            ejecutor.ValidarEscritura();

            _factory.RegistrarConfiguracionInicial(nombre, ruc, equipos, hid, ejecutor);
            return new ApiResponse<bool>(true, "Licencia registrada y sistema activado.", true);
        }
        catch (UnauthorizedAccessException ex)
        {
            return new ApiResponse<bool>(false, ex.Message, false);
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool>(false, $"Error en activación: {ex.Message}", false);
        }
    }

    /// <summary>
    /// Consulta el estado de la licencia para un equipo específico (No requiere ejecutor por ser pre-auth).
    /// </summary>
    public ApiResponse<LicenciaDTO> ConsultarLicencia(string hardwareId)
    {
        try
        {
            var licencia = _factory.ObtenerLicencia(hardwareId);
            if (licencia == null)
                return new ApiResponse<LicenciaDTO>(false, "Este equipo no cuenta con una licencia registrada.");

            return new ApiResponse<LicenciaDTO>(true, "Licencia encontrada.", licencia);
        }
        catch (Exception ex)
        {
            return new ApiResponse<LicenciaDTO>(false, $"Error al consultar licencia: {ex.Message}");
        }
    }
}