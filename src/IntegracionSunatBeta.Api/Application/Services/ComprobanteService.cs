using IntegracionSunatBeta.Api.Application.DTOs;
using IntegracionSunatBeta.Api.Application.Interfaces;
using IntegracionSunatBeta.Api.Application.Mappers;
using IntegracionSunatBeta.Api.Domain.Enums;
using IntegracionSunatBeta.Api.Domain.Exceptions;

namespace IntegracionSunatBeta.Api.Application.Services
{
    public class ComprobanteService : IComprobanteService
    {
        private readonly IComprobanteRepository _comprobanteRepository;
        private readonly ComprobanteMapper _mapper;

        public ComprobanteService(IComprobanteRepository comprobanteRepository, ComprobanteMapper mapper)
        {
            _comprobanteRepository = comprobanteRepository;
            _mapper = mapper;
        }

        public async Task<PagedResponse<ComprobanteResumenResponse>> ListarAsync(
            EstadoComprobante? estado,
            TipoComprobante? tipo,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default)
        {
            var paginaSegura = Math.Max(1, page);
            var tamanioSeguro = Math.Clamp(pageSize, 1, 100);
            var (items, totalCount) = await _comprobanteRepository.ListarAsync(
                estado, tipo, paginaSegura, tamanioSeguro, ct);
            var totalPages = totalCount == 0
                ? 0
                : (int)Math.Ceiling(totalCount / (double)tamanioSeguro);

            return new PagedResponse<ComprobanteResumenResponse>(
                items.Select(_mapper.ToResumenResponse).ToList(),
                paginaSegura,
                tamanioSeguro,
                totalCount,
                totalPages);
        }

        public async Task<ComprobanteDetalleResponse> ObtenerPorIdAsync(Guid id, CancellationToken ct = default)
        {
            var comprobante = await _comprobanteRepository.ObtenerPorIdConDetallesIntentosAsync(id, ct)
                ?? throw new DomainException($"No se encontró el comprobante con id '{id}'.", TipoErrorDominio.NoEncontrado);
            return _mapper.ToDetalleResponse(comprobante);
        }
    }
}