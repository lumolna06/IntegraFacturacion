using IntegraPro.AppLogic.Interfaces;
using IntegraPro.AppLogic.Utils;
using IntegraPro.DataAccess.Factory;
using IntegraPro.DTO.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace IntegraPro.AppLogic.Services;

public class UsuarioService : IUsuarioService
{
    private readonly UsuarioFactory _factory;
    private readonly LicenciaService _licenciaService;
    private readonly IConfiguration _config;

    public UsuarioService(UsuarioFactory factory, LicenciaService licenciaService, IConfiguration config)
    {
        _factory = factory;
        _licenciaService = licenciaService;
        _config = config;
    }

    public ApiResponse<UsuarioDTO> Login(string username, string password)
    {
        try
        {
            Guard.AgainstEmptyString(username, nameof(username));
            Guard.AgainstEmptyString(password, nameof(password));

            var usuario = _factory.GetByUsername(username);
            if (usuario == null || !usuario.Activo)
                return new ApiResponse<UsuarioDTO>(false, "Credenciales incorrectas o usuario inactivo");

            if (!PasswordHasher.VerifyPassword(password, usuario.PasswordHash))
            {
                Logger.WriteLog("Seguridad", "Login Fallido", $"Intento fallido: {username}");
                return new ApiResponse<UsuarioDTO>(false, "Credenciales incorrectas");
            }

            string hidActual = _licenciaService.GetHardwareId();
            if (!string.IsNullOrEmpty(usuario.HardwareIdSesion) && usuario.HardwareIdSesion != hidActual)
            {
                Logger.WriteLog("Seguridad", "Bloqueo Sesión", $"Usuario {username} intentó entrar desde otra PC.");
                return new ApiResponse<UsuarioDTO>(false, "SESION_ABIERTA_OTRO_EQUIPO");
            }

            _factory.ActualizarSesionHardware(usuario.Id, hidActual);
            _factory.RegistrarLogin(usuario.Id);

            // === GENERACIÓN DE TOKEN SINCRONIZADA ===
            usuario.Token = GenerarJwtToken(usuario);

            usuario.UltimoLogin = DateTime.Now;
            usuario.HardwareIdSesion = hidActual;
            usuario.PasswordHash = string.Empty;

            Logger.WriteLog("Seguridad", "Login Exitoso", $"Usuario {username} ha ingresado en equipo {hidActual}.");
            return new ApiResponse<UsuarioDTO>(true, "Acceso concedido", usuario);
        }
        catch (Exception ex)
        {
            Logger.WriteLog("Error", "Login", ex.Message);
            return new ApiResponse<UsuarioDTO>(false, $"Error interno: {ex.Message}");
        }
    }

    private string GenerarJwtToken(UsuarioDTO usuario)
    {
        // 1. LEEMOS LA CLAVE DIRECTO DE APPSETTINGS (Evitamos claves temporales fijas)
        var secretKey = _config["Jwt:Key"] ?? "EstaEsUnaClaveSecretaMuyLargaDeAlMenos32Caracteres2026!";
        var key = Encoding.ASCII.GetBytes(secretKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.Username),
                new Claim("RolId", usuario.RolId.ToString()),
                new Claim("SucursalId", usuario.SucursalId.ToString()),
                // Importante: Mandar el JSON de permisos para que el API lo reconstruya
                new Claim("Permisos", usuario.PermisosJson ?? "{}")
            }),
            Expires = DateTime.UtcNow.AddHours(12),
            // Aseguramos que el Issuer y Audience coincidan si están en el Program.cs
            Issuer = _config["Jwt:Issuer"],
            Audience = _config["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    // ... Resto de los métodos del servicio permanecen igual
    public ApiResponse<bool> Registrar(UsuarioDTO usuario, UsuarioDTO ejecutor)
    {
        try
        {
            Guard.AgainstNull(usuario, nameof(usuario));
            if (!ejecutor.TienePermiso("usuarios"))
                return new ApiResponse<bool>(false, "No tiene permisos para registrar usuarios.");

            if (ejecutor.TienePermiso("solo_lectura"))
                return new ApiResponse<bool>(false, "Operación denegada: Perfil de solo lectura.");

            var existente = _factory.GetByUsername(usuario.Username);
            if (existente != null)
                return new ApiResponse<bool>(false, "El nombre de usuario ya está en uso");

            if (usuario.RolId == 0) usuario.RolId = 2;
            usuario.PasswordHash = PasswordHasher.HashPassword(usuario.Password);
            _factory.Create(usuario, ejecutor);

            return new ApiResponse<bool>(true, "Usuario creado exitosamente", true);
        }
        catch (Exception ex)
        {
            Logger.WriteLog("Error", "Registro", ex.Message);
            return new ApiResponse<bool>(false, ex.Message);
        }
    }

    public ApiResponse<bool> Logout(int usuarioId)
    {
        try
        {
            _factory.ActualizarSesionHardware(usuarioId, null);
            return new ApiResponse<bool>(true, "Sesión cerrada correctamente", true);
        }
        catch (Exception ex)
        {
            Logger.WriteLog("Error", "Logout", ex.Message);
            return new ApiResponse<bool>(false, $"Error al cerrar sesión: {ex.Message}");
        }
    }

    public ApiResponse<bool> ForzarCierreSesion(string username, string password)
    {
        try
        {
            var usuario = _factory.GetByUsername(username);
            if (usuario != null && PasswordHasher.VerifyPassword(password, usuario.PasswordHash))
            {
                _factory.ActualizarSesionHardware(usuario.Id, null);
                return new ApiResponse<bool>(true, "Sesiones liberadas", true);
            }
            return new ApiResponse<bool>(false, "Credenciales inválidas.");
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool>(false, ex.Message);
        }
    }

    public ApiResponse<List<UsuarioDTO>> ObtenerTodos(UsuarioDTO ejecutor)
    {
        try
        {
            var usuarios = _factory.GetAll(ejecutor);
            usuarios.ForEach(u => u.PasswordHash = string.Empty);
            return new ApiResponse<List<UsuarioDTO>>(true, "Lista cargada", usuarios);
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<UsuarioDTO>>(false, ex.Message);
        }
    }

    public ApiResponse<bool> ActualizarRol(int usuarioId, int nuevoRolId, UsuarioDTO ejecutor)
    {
        try
        {
            if (!ejecutor.TienePermiso("usuarios") || ejecutor.TienePermiso("solo_lectura"))
                return new ApiResponse<bool>(false, "No tiene permisos para modificar roles.");

            _factory.ActualizarRol(usuarioId, nuevoRolId, ejecutor);
            return new ApiResponse<bool>(true, "Rol actualizado correctamente", true);
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool>(false, $"Error: {ex.Message}");
        }
    }

    public ApiResponse<List<RolDTO>> ListarRolesDisponibles(UsuarioDTO ejecutor)
    {
        try
        {
            return new ApiResponse<List<RolDTO>>(true, "Roles cargados", _factory.GetRoles(ejecutor));
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<RolDTO>>(false, ex.Message);
        }
    }
}