using IntegraPro.AppLogic.Interfaces;
using IntegraPro.AppLogic.Utils;
using IntegraPro.DataAccess.Factory;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Services;

public class UsuarioService : IUsuarioService
{
    private readonly UsuarioFactory _factory;
    private readonly LicenciaService _licenciaService;

    public UsuarioService(UsuarioFactory factory, LicenciaService licenciaService)
    {
        _factory = factory;
        _licenciaService = licenciaService;
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

            // 1. Verificar contraseña
            if (!PasswordHasher.VerifyPassword(password, usuario.PasswordHash))
            {
                Logger.WriteLog("Seguridad", "Login Fallido", $"Intento fallido: {username}");
                return new ApiResponse<UsuarioDTO>(false, "Credenciales incorrectas");
            }

            // 2. LÓGICA DE SESIÓN ÚNICA LIGADA AL HARDWARE
            string hidActual = _licenciaService.GetHardwareId();

            if (!string.IsNullOrEmpty(usuario.HardwareIdSesion) && usuario.HardwareIdSesion != hidActual)
            {
                Logger.WriteLog("Seguridad", "Bloqueo Sesión", $"Usuario {username} intentó entrar desde otra PC.");
                return new ApiResponse<UsuarioDTO>(false, "SESION_ABIERTA_OTRO_EQUIPO");
            }

            // 3. Registrar el Hardware ID en la sesión
            _factory.ActualizarSesionHardware(usuario.Id, hidActual);

            // 4. --- NUEVO: REGISTRAR FECHA Y HORA DEL ÚLTIMO LOGIN ---
            _factory.RegistrarLogin(usuario.Id);

            // Actualizamos el objeto en memoria para que la respuesta de la API no sea null
            usuario.UltimoLogin = DateTime.Now;
            usuario.HardwareIdSesion = hidActual;
            usuario.PasswordHash = string.Empty; // Seguridad: No devolver el hash

            Logger.WriteLog("Seguridad", "Login Exitoso", $"Usuario {username} ha ingresado en equipo {hidActual}.");

            return new ApiResponse<UsuarioDTO>(true, "Acceso concedido", usuario);
        }
        catch (Exception ex)
        {
            Logger.WriteLog("Error", "Login", ex.Message);
            return new ApiResponse<UsuarioDTO>(false, $"Error interno: {ex.Message}");
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
                Logger.WriteLog("Seguridad", "Forzar Cierre", $"Sesiones liberadas para el usuario: {username}");
                return new ApiResponse<bool>(true, "Sesiones liberadas. Ya puede reintentar el login.", true);
            }

            return new ApiResponse<bool>(false, "Credenciales inválidas para realizar esta acción.");
        }
        catch (Exception ex)
        {
            Logger.WriteLog("Error", "ForzarCierre", ex.Message);
            return new ApiResponse<bool>(false, $"Error al forzar cierre: {ex.Message}");
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

    public ApiResponse<bool> Registrar(UsuarioDTO usuario)
    {
        try
        {
            Guard.AgainstNull(usuario, nameof(usuario));
            Guard.AgainstEmptyString(usuario.Username, "Nombre de Usuario");

            if (string.IsNullOrEmpty(usuario.Password))
                return new ApiResponse<bool>(false, "La contraseña es obligatoria");

            var existente = _factory.GetByUsername(usuario.Username);
            if (existente != null)
                return new ApiResponse<bool>(false, "El nombre de usuario ya está en uso");

            // --- PROFESIONAL: ASIGNACIÓN DE ROL POR DEFECTO ---
            // Si no se especifica un RolId (0), asignamos el rol con ID 2 (Vendedor/Cajero por ej.)
            if (usuario.RolId == 0) usuario.RolId = 2;

            usuario.PasswordHash = PasswordHasher.HashPassword(usuario.Password);
            _factory.Create(usuario);

            return new ApiResponse<bool>(true, "Usuario creado exitosamente", true);
        }
        catch (Exception ex)
        {
            Logger.WriteLog("Error", "Registro", ex.Message);
            return new ApiResponse<bool>(false, ex.Message);
        }
    }

    // ==========================================
    // NUEVO: GESTIÓN ADMINISTRATIVA DE USUARIOS
    // ==========================================

    public ApiResponse<List<UsuarioDTO>> ObtenerTodos()
    {
        try
        {
            var usuarios = _factory.GetAll();
            // Limpiamos los hash por seguridad antes de enviarlos a la lista administrativa
            usuarios.ForEach(u => u.PasswordHash = string.Empty);
            return new ApiResponse<List<UsuarioDTO>>(true, "Lista cargada", usuarios);
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<UsuarioDTO>>(false, ex.Message);
        }
    }

    public ApiResponse<bool> ActualizarRol(int usuarioId, int nuevoRolId)
    {
        try
        {
            _factory.ActualizarRol(usuarioId, nuevoRolId);
            Logger.WriteLog("Administración", "Cambio de Rol", $"Usuario ID {usuarioId} ahora tiene Rol ID {nuevoRolId}");
            return new ApiResponse<bool>(true, "Rol actualizado correctamente", true);
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool>(false, $"Error al actualizar rol: {ex.Message}");
        }
    }

    public ApiResponse<List<RolDTO>> ListarRolesDisponibles()
    {
        try
        {
            // Este método debe existir en tu Factory para llenar los combos del Admin
            return new ApiResponse<List<RolDTO>>(true, "Roles cargados", _factory.GetRoles());
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<RolDTO>>(false, ex.Message);
        }
    }
}