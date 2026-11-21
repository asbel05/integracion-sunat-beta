using IntegracionSunatBeta.Api.Application.DTOs;

namespace IntegracionSunatBeta.Api.Application.Interfaces
{
    public interface IPedidoService
    {
        Task<PedidoDetalleResponse> CrearAsync(CrearPedidoRequest request, CancellationToken ct = default);
        Task<PedidoDetalleResponse> ObtenerPorIdAsync(Guid id, CancellationToken ct = default);
    }
}
