using IntegraPro.AppLogic.Utils;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Interfaces;

public interface IConfiguracionService
{
    // ACTUALIZADO: Ahora requiere el ejecutor para validar permisos de lectura del módulo 'config'
    ApiResponse<EmpresaDTO> ObtenerDatosEmpresa(UsuarioDTO ejecutor);

    ApiResponse<bool> ActualizarEmpresa(EmpresaDTO empresa, UsuarioDTO ejecutor);

    ApiResponse<bool> RegistrarLicenciaInicial(string nombre, string ruc, int equipos, string hid, UsuarioDTO ejecutor);

    // Se mantiene sin ejecutor porque se consulta usualmente antes del login (validación de equipo)
    ApiResponse<LicenciaDTO> ConsultarLicencia(string hardwareId);
}