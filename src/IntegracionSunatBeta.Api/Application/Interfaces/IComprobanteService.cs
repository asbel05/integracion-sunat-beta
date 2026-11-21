using IntegracionSunatBeta.Api.Application.DTOs;
using IntegracionSunatBeta.Api.Domain.Enums;

namespace IntegracionSunatBeta.Api.Application.Interfaces
{
    public interface IComprobanteService
    {
        Task<PagedResponse<ComprobanteResumenResponse>> ListarAsync(
            EstadoComprobante? estado,
            TipoComprobante? tipo,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default);
        Task<ComprobanteDetalleResponse> ObtenerPorIdAsync(Guid id, CancellationToken ct = default);
    }
}