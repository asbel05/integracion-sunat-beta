using IntegracionSunatBeta.Api.Domain.Enums;

namespace IntegracionSunatBeta.Api.Application.DTOs
{
    public record ComprobanteResumenResponse(
        Guid Id,
        Guid PedidoId,
        string PedidoCodigo,
        string ClienteTipoDoc,
        string ClienteNumDoc,
        string ClienteNombre,
        decimal Total,
        TipoComprobante Tipo,
        string Serie,
        int Correlativo,
        string NumeroCompleto,
        DateOnly FechaEmision,
        EstadoComprobante Estado,
        int Intentos,
        string? CodigoCdr,
        string? MensajeSunat,
        DateTime ActualizadoUtc);
    public record ComprobanteIntentoResponse(
        long Id,
        int NumeroIntento,
        string Resultado,
        string? DetalleError,
        DateTime FechaUtc);
    public record ComprobanteDetalleResponse(
        Guid Id,
        Guid PedidoId,
        string PedidoCodigo,
        string ClienteTipoDoc,
        string ClienteNumDoc,
        string ClienteNombre,
        decimal Subtotal,
        decimal Igv,
        decimal Total,
        TipoComprobante Tipo,
        string Serie,
        int Correlativo,
        string NumeroCompleto,
        DateOnly FechaEmision,
        EstadoComprobante Estado,
        int Intentos,
        string? CodigoCdr,
        string? MensajeSunat,
        DateTime ActualizadoUtc,
        IReadOnlyList<PedidoItemResponse> Detalles,
        IReadOnlyList<ComprobanteIntentoResponse> HistorialIntentos);

    public record EnviarComprobantesRequest(List<Guid> ComprobanteIds);

    public record EnvioEncoladoResponse(
        int Encolados,
        IReadOnlyList<Guid> ComprobanteIds,
        string Estado);
}
