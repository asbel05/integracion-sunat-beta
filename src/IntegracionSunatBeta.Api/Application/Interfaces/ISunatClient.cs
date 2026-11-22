using IntegracionSunatBeta.Api.Domain.Entities;

namespace IntegracionSunatBeta.Api.Application.Interfaces
{
    public enum TipoResultadoSunat
    {
        Aceptado = 1,
        Rechazado = 2,
        ErrorTransitorio = 3
    }

    public record SunatEnvioResultado(TipoResultadoSunat Tipo, string? CodigoCdr, string? Mensaje);

    public interface ISunatClient
    {
        Task<SunatEnvioResultado> EnviarComprobanteAsync(Comprobante comprobante, CancellationToken ct = default);
    }
}
