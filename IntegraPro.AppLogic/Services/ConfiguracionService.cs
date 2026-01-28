using IntegraPro.AppLogic.Interfaces;
using IntegraPro.AppLogic.Utils;
using IntegraPro.DataAccess.Factory;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Services;

public class ConfiguracionService(ConfiguracionFactory factory, UsuarioFactory usuarioFactory) : IConfiguracionService
{
    private readonly ConfiguracionFactory _factory = factory;
    private readonly UsuarioFactory _usuarioFactory = usuarioFactory;

    public ApiResponse<bool> FinalizarInstalacion(RegistroInicialDTO m)
    {
        try
        {
            // Creamos un ejecutor virtual con permisos para saltar las validaciones del Factory
            var ejecutorSistema = new UsuarioDTO();
            ejecutorSistema.Permisos.Add("config", true);
            ejecutorSistema.Permisos.Add("usuarios", true);

            // 1. Guardar o Actualizar Empresa
            var empresa = new EmpresaDTO
            {
                NombreComercial = m.NombreComercial,
                RazonSocial = string.IsNullOrEmpty(m.RazonSocial) ? m.NombreComercial : m.RazonSocial,
                CedulaJuridica = m.CedulaJuridica,
                TipoRegimen = m.TipoRegimen,
                Telefono = m.Telefono,
                CorreoNotificaciones = m.CorreoElectronico,
                PermitirStockNegativo = m.PermitirStockNegativo
            };
            _factory.GuardarEmpresa(empresa, ejecutorSistema);

            // 2. Crear Sucursal por defecto vinculada a la empresa (Evita error de Llave Foránea)
            _factory.CrearSucursalBase(1);

            // 3. Crear Usuario Administrador con Password Cifrado
            var admin = new UsuarioDTO
            {
                Username = m.Username,
                // APLICAMOS EL HASH PROFESIONAL AQUÍ:
                PasswordHash = PasswordHasher.HashPassword(m.Password),
                NombreCompleto = m.NombreCompleto,
                CorreoElectronico = m.CorreoElectronico,
                RolId = 1,      // ID de Rol Administrador
                SucursalId = 1, // ID de la sucursal creada arriba
                Activo = true
            };
            _usuarioFactory.Create(admin, ejecutorSistema);

            return new ApiResponse<bool>(true, "¡Instalación completada con éxito!", true);
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }

    // --- Métodos de la Interfaz ---
    public ApiResponse<EmpresaDTO> ObtenerDatosEmpresa(UsuarioDTO e) => new ApiResponse<EmpresaDTO>(true, "", _factory.ObtenerEmpresa(e));

    public ApiResponse<int> RegistrarEmpresa(EmpresaDTO d, UsuarioDTO? e)
    {
        _factory.GuardarEmpresa(d, e ?? new UsuarioDTO());
        return new ApiResponse<int>(true, "Empresa registrada", 1);
    }

    public ApiResponse<bool> ActualizarEmpresa(EmpresaDTO d, UsuarioDTO e)
    {
        _factory.GuardarEmpresa(d, e);
        return new ApiResponse<bool>(true, "Empresa actualizada", true);
    }

    public ApiResponse<bool> RegistrarLicenciaInicial(string n, string r, int q, string h, UsuarioDTO e) => new ApiResponse<bool>(true, "Licencia registrada", true);

    public ApiResponse<LicenciaDTO> ConsultarLicencia(string h) => new ApiResponse<LicenciaDTO>(true, "Licencia consultada", new LicenciaDTO());
}