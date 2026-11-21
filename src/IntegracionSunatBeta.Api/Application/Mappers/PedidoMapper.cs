using IntegracionSunatBeta.Api.Application.DTOs;
using IntegracionSunatBeta.Api.Domain.Entities;

namespace IntegracionSunatBeta.Api.Application.Mappers
{
    public class PedidoMapper
    {
        public PedidoDetalleResponse ToDetalleResponse(Pedido pedido)
        {
            var comprobante = pedido.Comprobante ?? throw new InvalidOperationException($"El pedido {pedido.Id} no tiene comprobante cargado");

            return new PedidoDetalleResponse(
                pedido.Id,
                pedido.Codigo,
                pedido.ClienteTipoDoc,
                pedido.ClienteNumDoc,
                pedido.ClienteNombre,
                pedido.Subtotal,
                pedido.Igv,
                pedido.Total,
                pedido.FechaCreacionUtc,
                pedido.Detalles.Select(d => new PedidoItemResponse(
                    d.Id,
                    d.Descripcion,
                    d.Cantidad,
                    d.PrecioUnitario,
                    d.Subtotal)).ToList(),
                new ComprobanteEnPedidoResponse(
                    comprobante.Id,
                    comprobante.Tipo,
                    comprobante.Serie,
                    comprobante.Correlativo,
                    comprobante.NumeroCompleto,
                    comprobante.FechaEmision,
                    comprobante.Estado));
        }
    }
}
