using IntegraPro.AppLogic.Interfaces;
using IntegraPro.AppLogic.Utils;
using IntegraPro.DataAccess.Factory;
using IntegraPro.DTO.Models;
using System;
using System.Collections.Generic;

namespace IntegraPro.AppLogic.Services;

public class InventarioService(InventarioFactory factory, ProductoFactory productoFactory) : IInventarioService
{
    public ApiResponse<bool> Registrar(MovimientoInventarioDTO mov, UsuarioDTO ejecutor)
    {
        try
        {
            // 1. VALIDACIÓN DE SEGURIDAD
            if (ejecutor.TienePermiso("solo_lectura"))
                return new ApiResponse<bool>(false, "No tiene permisos para modificar el inventario.", false);

            if (mov.Cantidad <= 0)
                return new ApiResponse<bool>(false, "La cantidad debe ser mayor a cero", false);

            // Forzamos que el movimiento sea en la sucursal del ejecutor si tiene límites
            if (ejecutor.TienePermiso("sucursal_limit"))
                mov.SucursalId = ejecutor.SucursalId;

            mov.UsuarioId = ejecutor.Id; // Auditoría garantizada

            bool ok = factory.Insertar(mov, ejecutor);
            return new ApiResponse<bool>(ok, ok ? "Movimiento registrado y stock actualizado" : "Error al registrar", ok);
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }

    public ApiResponse<bool> ProcesarProduccion(int productoPadreId, decimal cantidadAProducir, UsuarioDTO ejecutor)
    {
        try
        {
            // 1. SEGURIDAD Y PRE-REQUISITOS
            if (ejecutor.TienePermiso("solo_lectura"))
                throw new UnauthorizedAccessException("Su perfil no permite procesar producción.");

            if (cantidadAProducir <= 0)
                return new ApiResponse<bool>(false, "La cantidad a producir debe ser mayor a cero", false);

            // Obtener la receta del producto padre
            var receta = productoFactory.ObtenerComposicion(productoPadreId);

            if (receta == null || receta.Count == 0)
                return new ApiResponse<bool>(false, "El producto no tiene receta definida o no es elaborado.", false);

            // 2. REGISTRAR SALIDAS DE MATERIAS PRIMAS (Explosión de materiales)
            foreach (var item in receta)
            {
                var movSalida = new MovimientoInventarioDTO
                {
                    ProductoId = item.MaterialId,
                    UsuarioId = ejecutor.Id,
                    SucursalId = ejecutor.SucursalId, // Producción ocurre en la sucursal del usuario
                    TipoMovimiento = "SALIDA",
                    Cantidad = item.CantidadNecesaria * cantidadAProducir,
                    DocumentoReferencia = $"PROD-P{productoPadreId}",
                    Notas = $"Consumo automático para producir {cantidadAProducir} unidades del padre ID {productoPadreId}"
                };

                // Enviamos el ejecutor para que el Factory valide existencias en esa sucursal
                if (!factory.Insertar(movSalida, ejecutor))
                    throw new Exception($"Error al rebajar stock del material ID {item.MaterialId}. ¿Hay stock suficiente?");
            }

            // 3. REGISTRAR ENTRADA DEL PRODUCTO TERMINADO
            var movEntrada = new MovimientoInventarioDTO
            {
                ProductoId = productoPadreId,
                UsuarioId = ejecutor.Id,
                SucursalId = ejecutor.SucursalId,
                TipoMovimiento = "ENTRADA",
                Cantidad = cantidadAProducir,
                DocumentoReferencia = "ORDEN-PROD",
                Notas = $"Ingreso por finalización de proceso productivo"
            };

            if (!factory.Insertar(movEntrada, ejecutor))
                throw new Exception("Error al ingresar el stock del producto terminado.");

            return new ApiResponse<bool>(true, $"Producción exitosa: {cantidadAProducir} unidades cargadas a sucursal {ejecutor.SucursalId}.", true);
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool>(false, "Error en producción: " + ex.Message, false);
        }
    }
}