using IntegraPro.AppLogic.Utils;
using IntegraPro.DTO.Models;
using System.Collections.Generic;

namespace IntegraPro.AppLogic.Interfaces;

public interface ICategoriaService
{
    // ACTUALIZADO: Se añade el ejecutor para validar permisos de acceso al módulo
    ApiResponse<List<CategoriaDTO>> ObtenerTodas(UsuarioDTO ejecutor);

    // Mantenemos el ejecutor para validación de roles y escritura
    ApiResponse<bool> Crear(CategoriaDTO categoria, UsuarioDTO ejecutor);
}