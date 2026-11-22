using FluentValidation;
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
        private readonly IComprobanteQueuePublisher _queuePublisher;
        private readonly IValidator<EnviarComprobantesRequest> _enviarValidator;
        private readonly ILogger<ComprobanteService> _logger;

        public ComprobanteService(
            IComprobanteRepository comprobanteRepository,
            ComprobanteMapper mapper,
            IComprobanteQueuePublisher queuePublisher,
            IValidator<EnviarComprobantesRequest> enviarValidator,
            ILogger<ComprobanteService> logger)
        {
            _comprobanteRepository = comprobanteRepository;
            _mapper = mapper;
            _queuePublisher = queuePublisher;
            _enviarValidator = enviarValidator;
            _logger = logger;
        }

        public async Task<PagedResponse<ComprobanteResumenResponse>> ListarAsync(
            EstadoComprobante? estado, TipoComprobante? tipo, int page = 1, int pageSize = 20, CancellationToken ct = default)
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

        public async Task<EnvioEncoladoResponse> EnviarAsync(EnviarComprobantesRequest request, CancellationToken ct = default)
        {
            await _enviarValidator.ValidateAndThrowAsync(request, ct);

            var comprobantes = await _comprobanteRepository.ObtenerPorIdsAsync(request.ComprobanteIds, ct);
            // validar que todos existan (404)
            if (comprobantes.Count != request.ComprobanteIds.Count)
            {
                var encontrados = comprobantes.Select(c => c.Id).ToHashSet();
                var faltantes = request.ComprobanteIds.Where(id => !encontrados.Contains(id));
                throw new DomainException(
                    $"No se encontraron los comprobantes: {string.Join(", ", faltantes)}.",
                    TipoErrorDominio.NoEncontrado);
            }

            // validar que todos estén en SinEnviar (409)
            var invalidos = comprobantes.Where(c => c.Estado != EstadoComprobante.SinEnviar).ToList();

            if (invalidos.Count > 0)
            {
                var detalle = string.Join(", ", invalidos.Select(c => $"{c.NumeroCompleto} ({c.Estado})"));
                throw new DomainException(
                    $"Solo se pueden enviar comprobantes en estado 'SinEnviar'. Conflictos: {detalle}.",
                    TipoErrorDominio.ConflictoEstado);
            }

            var ahoraUtc = DateTime.UtcNow;

            foreach (var comprobante in comprobantes)
            {
                comprobante.Estado = EstadoComprobante.EnCola;
                comprobante.ActualizadoUtc = ahoraUtc;
            }

            await _comprobanteRepository.GuardarCambiosAsync(ct);

            try
            {
                await _queuePublisher.PublicarAsync(request.ComprobanteIds, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publicando lote en Service Bus. Revirtiendo {Count} comprobantes a SinEnviar.", comprobantes.Count);
                foreach (var comprobante in comprobantes)
                {
                    comprobante.Estado = EstadoComprobante.SinEnviar;
                    comprobante.ActualizadoUtc = DateTime.UtcNow;
                }
                await _comprobanteRepository.GuardarCambiosAsync(CancellationToken.None);
                throw new DomainException(
                    "No se pudo publicar el lote en Azure Service Bus. Los comprobantes volvieron a 'SinEnviar'.",
                    TipoErrorDominio.ServicioNoDisponible);
            }
            return new EnvioEncoladoResponse(
                comprobantes.Count,
                request.ComprobanteIds,
                nameof(EstadoComprobante.EnCola));
        }

        public async Task<EnvioEncoladoResponse> ReintentarAsync(Guid id, CancellationToken ct = default)
        {
            var comprobante = await _comprobanteRepository.ObtenerPorIdConDetallesIntentosAsync(id, ct)
                ?? throw new DomainException($"No se encontró el comprobante con id '{id}'.", TipoErrorDominio.NoEncontrado);

            if (comprobante.Estado != EstadoComprobante.ErrorEnvio)
            {
                throw new DomainException(
                    $"Solo se puede reintentar un comprobante en estado 'ErrorEnvio'. Estado actual: '{comprobante.Estado}'.",
                    TipoErrorDominio.ConflictoEstado);
            }
            comprobante.Estado = EstadoComprobante.EnCola;
            comprobante.ActualizadoUtc = DateTime.UtcNow;

            await _comprobanteRepository.GuardarCambiosAsync(ct);

            try
            {
                await _queuePublisher.PublicarAsync([comprobante.Id], ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error republicando comprobante {Id} en Service Bus. Revirtiendo a ErrorEnvio.", comprobante.Id);
                comprobante.Estado = EstadoComprobante.ErrorEnvio;
                comprobante.ActualizadoUtc = DateTime.UtcNow;
                await _comprobanteRepository.GuardarCambiosAsync(CancellationToken.None);
                throw new DomainException(
                    "No se pudo republicar el comprobante en Azure Service Bus. Volvió a 'ErrorEnvio'.",
                    TipoErrorDominio.ServicioNoDisponible);
            }

            return new EnvioEncoladoResponse(1, [comprobante.Id], nameof(EstadoComprobante.EnCola));
        }
    }
}