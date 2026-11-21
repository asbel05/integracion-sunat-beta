using IntegracionSunatBeta.Api.Application.DTOs;
using IntegracionSunatBeta.Api.Domain.Entities;

namespace IntegracionSunatBeta.Api.Application.Mappers
{
    public class ComprobanteMapper
    {
        public ComprobanteResumenResponse ToResumenResponse(Comprobante comprobante)
        {
            var pedido = comprobante.Pedido
                ?? throw new InvalidOperationException($"El comprobante {comprobante.Id} no tiene pedido cargado.");
            return new ComprobanteResumenResponse(
                comprobante.Id,
                comprobante.PedidoId,
                pedido.Codigo,
                pedido.ClienteTipoDoc,
                pedido.ClienteNumDoc,
                pedido.ClienteNombre,
                pedido.Total,
                comprobante.Tipo,
                comprobante.Serie,
                comprobante.Correlativo,
                comprobante.NumeroCompleto,
                comprobante.FechaEmision,
                comprobante.Estado,
                comprobante.Intentos,
                comprobante.CodigoCdr,
                comprobante.MensajeSunat,
                comprobante.ActualizadoUtc);
        }
        public ComprobanteDetalleResponse ToDetalleResponse(Comprobante comprobante)
        {
            var pedido = comprobante.Pedido
                ?? throw new InvalidOperationException($"El comprobante {comprobante.Id} no tiene pedido cargado.");
            return new ComprobanteDetalleResponse(
                comprobante.Id,
                comprobante.PedidoId,
                pedido.Codigo,
                pedido.ClienteTipoDoc,
                pedido.ClienteNumDoc,
                pedido.ClienteNombre,
                pedido.Subtotal,
                pedido.Igv,
                pedido.Total,
                comprobante.Tipo,
                comprobante.Serie,
                comprobante.Correlativo,
                comprobante.NumeroCompleto,
                comprobante.FechaEmision,
                comprobante.Estado,
                comprobante.Intentos,
                comprobante.CodigoCdr,
                comprobante.MensajeSunat,
                comprobante.ActualizadoUtc,
                pedido.Detalles.Select(d => new PedidoItemResponse(
                    d.Id,
                    d.Descripcion,
                    d.Cantidad,
                    d.PrecioUnitario,
                    d.Subtotal)).ToList(),
                comprobante.HistorialIntentos.Select(i => new ComprobanteIntentoResponse(
                    i.Id,
                    i.NumeroIntento,
                    i.Resultado,
                    i.DetalleError,
                    i.FechaUtc)).ToList());
        }
    }
}