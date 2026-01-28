using IntegraPro.AppLogic.Utils;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Interfaces;

public interface IConfiguracionService
{
    ApiResponse<EmpresaDTO> ObtenerDatosEmpresa(UsuarioDTO ejecutor);
    ApiResponse<int> RegistrarEmpresa(EmpresaDTO empresa, UsuarioDTO? ejecutor);
    ApiResponse<bool> ActualizarEmpresa(EmpresaDTO empresa, UsuarioDTO ejecutor);

    // MÉTODO MAESTRO PARA EL REGISTRO INICIAL
    ApiResponse<bool> FinalizarInstalacion(RegistroInicialDTO modelo);

    ApiResponse<bool> RegistrarLicenciaInicial(string nombre, string ruc, int equipos, string hid, UsuarioDTO ejecutor);
    ApiResponse<LicenciaDTO> ConsultarLicencia(string hardwareId);
}