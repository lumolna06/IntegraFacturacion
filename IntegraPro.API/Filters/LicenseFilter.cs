using IntegraPro.AppLogic.Services;
using IntegraPro.DataAccess.Factory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace IntegraPro.API.Filters;

public class LicenseFilter : IAuthorizationFilter
{
    private readonly LicenciaService _licenciaService;
    private readonly UsuarioFactory _usuarioFactory;

    public LicenseFilter(LicenciaService licenciaService, UsuarioFactory usuarioFactory)
    {
        _licenciaService = licenciaService;
        _usuarioFactory = usuarioFactory;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // 1. VALIDACIÓN DE HARDWARE (¿La PC tiene permiso de usar el software?)
        var validacionHardware = _licenciaService.ValidarSistema();
        string hidActual = _licenciaService.GetHardwareId();

        if (!validacionHardware.Result)
        {
            context.Result = new ObjectResult(new
            {
                Result = false,
                Message = "EQUIPO_NO_AUTORIZADO",
                Details = "Esta computadora no cuenta con una licencia activa."
            })
            { StatusCode = 403 };
            return;
        }

        // 2. VALIDACIÓN DE SESIÓN ÚNICA (¿Es este usuario el que se logueó en esta PC?)
        // Extraemos el nombre de usuario del Token/Identidad actual
        var username = context.HttpContext.User.Identity?.Name;

        if (!string.IsNullOrEmpty(username))
        {
            var usuario = _usuarioFactory.GetByUsername(username);

            if (usuario != null && !string.IsNullOrEmpty(usuario.HardwareIdSesion))
            {
                if (usuario.HardwareIdSesion != hidActual)
                {
                    context.Result = new ObjectResult(new
                    {
                        Result = false,
                        Message = "SESION_INVALIDA_OTRO_EQUIPO",
                        Details = "Tu sesión ha sido abierta en otra computadora. Se ha cerrado el acceso aquí."
                    })
                    { StatusCode = 401 };
                }
            }
        }
    }
}