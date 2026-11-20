using IntegracionSunatBeta.Api.Domain.Enums;

namespace IntegracionSunatBeta.Api.Application.DTOs
{
    public record CrearPedidoRequest(
        string ClienteTipoDoc,
        string ClienteNumDoc,
        string ClienteNombre,
        TipoComprobante TipoComprobante,
        List<CrearPedidoDetalleRequest> Detalles);
    public record CrearPedidoDetalleRequest(
        string Descripcion,
        int Cantidad,
        decimal PrecioUnitario);
    public record PedidoItemResponse(
        Guid Id,
        string Descripcion,
        int Cantidad,
        decimal PrecioUnitario,
        decimal Subtotal);
    public record ComprobanteEnPedidoResponse(
        Guid Id,
        TipoComprobante Tipo,
        string Serie,
        int Correlativo,
        string NumeroCompleto,
        DateOnly FechaEmision,
        EstadoComprobante Estado);
    public record PedidoDetalleResponse(
        Guid Id,
        string Codigo,
        string ClienteTipoDoc,
        string ClienteNumDoc,
        string ClienteNombre,
        decimal Subtotal,
        decimal Igv,
        decimal Total,
        DateTime FechaCreacionUtc,
        IReadOnlyList<PedidoItemResponse> Detalles,
        ComprobanteEnPedidoResponse Comprobante);
}
