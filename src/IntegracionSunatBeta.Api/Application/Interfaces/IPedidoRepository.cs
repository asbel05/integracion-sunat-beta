using IntegracionSunatBeta.Api.Domain.Entities;

namespace IntegracionSunatBeta.Api.Application.Interfaces
{
    public interface IPedidoRepository
    {
        Task<int> ObtenerSiguienteCodigoAsync(CancellationToken ct = default);
        Task<int> ObtenerSiguienteCorrelativoAsync(string serie, CancellationToken ct = default);
        Task<Pedido?> ObtenerPorIdConDetalleAsync(Guid id, CancellationToken ct = default);
        Task AgregarAsync(Pedido pedido, CancellationToken ct = default);
        Task GuardarCambiosAsync(CancellationToken ct = default);
    }
}
