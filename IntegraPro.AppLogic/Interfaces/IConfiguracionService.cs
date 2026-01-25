using IntegraPro.AppLogic.Utils;
using IntegraPro.DTO.Models;
using System.Collections.Generic;

namespace IntegraPro.AppLogic.Interfaces;

public interface IConfiguracionService
{
    ApiResponse<EmpresaDTO> ObtenerDatosEmpresa();
    ApiResponse<bool> ActualizarEmpresa(EmpresaDTO empresa, UsuarioDTO ejecutor);
    ApiResponse<bool> RegistrarLicenciaInicial(string nombre, string ruc, int equipos, string hid, UsuarioDTO ejecutor);
    ApiResponse<LicenciaDTO> ConsultarLicencia(string hardwareId);
}