using IntegracionSunatBeta.Api.Domain.Entities;
using IntegracionSunatBeta.Api.Domain.Enums;

namespace IntegracionSunatBeta.Api.Application.Interfaces
{
    public interface IComprobanteRepository
    {
        Task<(IReadOnlyList<Comprobante> Items, int TotalCount)> ListarAsync(
            EstadoComprobante? estado = null, TipoComprobante? tipo = null,
            int page = 1, int pageSize = 20, CancellationToken ct = default);

        Task<Comprobante?> ObtenerPorIdConDetallesIntentosAsync(Guid id, CancellationToken ct = default);
        Task AgregarIntentoAsync(ComprobanteIntento intento, CancellationToken ct = default);
        Task GuardarCambiosAsync(CancellationToken ct = default);
        Task<List<Comprobante>> ObtenerPorIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
    }
}
