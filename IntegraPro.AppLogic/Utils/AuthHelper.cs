using System.Text.Json;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Utils
{
    public static class AuthHelper
    {
        public static PermisosDetalle ObtenerPermisos(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new PermisosDetalle();

            try
            {
                return JsonSerializer.Deserialize<PermisosDetalle>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new PermisosDetalle();
            }
            catch
            {
                return new PermisosDetalle();
            }
        }

        // Método para verificar si el usuario tiene permiso para un módulo
        public static bool TieneAcceso(PermisosDetalle permisos, string modulo)
        {
            if (permisos.All) return true; // El admin siempre entra

            return modulo.ToLower() switch
            {
                "ventas" => permisos.Ventas,
                "inventario" => permisos.Inventario,
                "compras" => permisos.Compras,
                "caja" => permisos.Caja,
                _ => false
            };
        }
    }
}
