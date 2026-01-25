using IntegraPro.AppLogic.Interfaces;
using IntegraPro.AppLogic.Utils;
using IntegraPro.DataAccess.Factory;
using IntegraPro.DTO.Models;
using System;
using System.Collections.Generic;

namespace IntegraPro.AppLogic.Services;

public class InventarioService(InventarioFactory factory, ProductoFactory productoFactory) : IInventarioService
{
    private readonly InventarioFactory _factory = factory;
    private readonly ProductoFactory _productoFactory = productoFactory;

    public ApiResponse<bool> Registrar(MovimientoInventarioDTO mov, UsuarioDTO ejecutor)
    {
        try
        {
            // SEGURIDAD: Usamos los helpers estandarizados
            ejecutor.ValidarAcceso("inventario");
            ejecutor.ValidarEscritura();

            if (mov.Cantidad <= 0)
                return new ApiResponse<bool>(false, "La cantidad debe ser mayor a cero", false);

            // La lógica de sucursal_limit ya está blindada en el Factory, 
            // pero mantenerla aquí como pre-validación es buena práctica.
            if (ejecutor.TienePermiso("sucursal_limit"))
                mov.SucursalId = ejecutor.SucursalId;

            bool ok = _factory.Insertar(mov, ejecutor);
            return new ApiResponse<bool>(ok, ok ? "Movimiento registrado y stock actualizado" : "Error al registrar", ok);
        }
        catch (UnauthorizedAccessException ex) { return new ApiResponse<bool>(false, ex.Message, false); }
        catch (Exception ex) { return new ApiResponse<bool>(false, $"Error: {ex.Message}", false); }
    }

    /// <summary>
    /// Procesa la producción rebajando materias primas e ingresando producto terminado.
    /// </summary>
    public ApiResponse<bool> ProcesarProduccion(int productoPadreId, decimal cantidadAProducir, UsuarioDTO ejecutor)
    {
        try
        {
            ejecutor.ValidarAcceso("inventario");
            ejecutor.ValidarEscritura();

            if (cantidadAProducir <= 0)
                return new ApiResponse<bool>(false, "Cantidad inválida", false);

            var receta = _productoFactory.ObtenerComposicion(productoPadreId);
            if (receta == null || receta.Count == 0)
                return new ApiResponse<bool>(false, "El producto no tiene receta definida.", false);

            // NOTA: Aquí se recomienda implementar un TransactionScope en producción
            // para asegurar que si falla el paso 3, se revierta el paso 2.

            // 2. EXPLOSIÓN DE MATERIALES (Salidas)
            foreach (var item in receta)
            {
                var movSalida = new MovimientoInventarioDTO
                {
                    ProductoId = item.MaterialId,
                    UsuarioId = ejecutor.Id,
                    SucursalId = ejecutor.SucursalId,
                    TipoMovimiento = "SALIDA",
                    Cantidad = item.CantidadNecesaria * cantidadAProducir,
                    DocumentoReferencia = $"PROD-P{productoPadreId}",
                    Notas = $"Consumo automático para producción."
                };

                if (!_factory.Insertar(movSalida, ejecutor))
                    throw new Exception($"Stock insuficiente para material ID {item.MaterialId}.");
            }

            // 3. ENTRADA PRODUCTO TERMINADO
            var movEntrada = new MovimientoInventarioDTO
            {
                ProductoId = productoPadreId,
                UsuarioId = ejecutor.Id,
                SucursalId = ejecutor.SucursalId,
                TipoMovimiento = "ENTRADA",
                Cantidad = cantidadAProducir,
                DocumentoReferencia = "ORDEN-PROD",
                Notas = $"Ingreso por producción"
            };

            _factory.Insertar(movEntrada, ejecutor);

            return new ApiResponse<bool>(true, $"Producción finalizada exitosamente.", true);
        }
        catch (UnauthorizedAccessException ex) { return new ApiResponse<bool>(false, ex.Message, false); }
        catch (Exception ex) { return new ApiResponse<bool>(false, "Error en producción: " + ex.Message, false); }
    }
}