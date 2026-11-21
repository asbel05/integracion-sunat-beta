using FluentValidation;
using IntegracionSunatBeta.Api.Application.DTOs;
using IntegracionSunatBeta.Api.Application.Interfaces;
using IntegracionSunatBeta.Api.Application.Mappers;
using IntegracionSunatBeta.Api.Domain.Entities;
using IntegracionSunatBeta.Api.Domain.Enums;
using IntegracionSunatBeta.Api.Domain.Exceptions;

namespace IntegracionSunatBeta.Api.Application.Services
{
    public class PedidoService : IPedidoService
    {
        private const decimal TasaIgv = 0.18m;
        private readonly IPedidoRepository _pedidoRepository;
        private readonly IValidator<CrearPedidoRequest> _validator;
        private readonly PedidoMapper _mapper;

        public PedidoService(IPedidoRepository pedidoRepository, IValidator<CrearPedidoRequest> validator, PedidoMapper mapper)
        {
            _pedidoRepository = pedidoRepository;
            _validator = validator;
            _mapper = mapper;
        }

        public async Task<PedidoDetalleResponse> CrearAsync(CrearPedidoRequest request, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(request, ct);
            var ahoraUtc = DateTime.UtcNow;
            var pedidoId = Guid.CreateVersion7();
            var comprobanteId = Guid.CreateVersion7();
            var serie = request.TipoComprobante == TipoComprobante.Factura ? "F001" : "B001";
            var numeroPedido = await _pedidoRepository.ObtenerSiguienteCodigoAsync(ct);
            var correlativo = await _pedidoRepository.ObtenerSiguienteCorrelativoAsync(serie, ct);
            var detalles = request.Detalles.Select(item => new PedidoDetalle
            {
                Id = Guid.CreateVersion7(),
                PedidoId = pedidoId,
                Descripcion = item.Descripcion.Trim(),
                Cantidad = item.Cantidad,
                PrecioUnitario = item.PrecioUnitario,
                Subtotal = Math.Round(item.Cantidad * item.PrecioUnitario, 2, MidpointRounding.AwayFromZero)
            }).ToList();
            var subtotal = Math.Round(detalles.Sum(d => d.Subtotal), 2, MidpointRounding.AwayFromZero);
            var igv = Math.Round(subtotal * TasaIgv, 2, MidpointRounding.AwayFromZero);
            var total = subtotal + igv;
            var comprobante = new Comprobante
            {
                Id = comprobanteId,
                PedidoId = pedidoId,
                Tipo = request.TipoComprobante,
                Serie = serie,
                Correlativo = correlativo,
                FechaEmision = ObtenerFechaPeru(ahoraUtc),
                Estado = EstadoComprobante.SinEnviar,
                Intentos = 0,
                ActualizadoUtc = ahoraUtc
            };
            var pedido = new Pedido
            {
                Id = pedidoId,
                Codigo = $"PED-{numeroPedido:D4}",
                ClienteTipoDoc = request.ClienteTipoDoc.Trim(),
                ClienteNumDoc = request.ClienteNumDoc.Trim(),
                ClienteNombre = request.ClienteNombre.Trim(),
                Subtotal = subtotal,
                Igv = igv,
                Total = total,
                FechaCreacionUtc = ahoraUtc,
                Detalles = detalles,
                Comprobante = comprobante
            };

            await _pedidoRepository.AgregarAsync(pedido, ct);
            await _pedidoRepository.GuardarCambiosAsync(ct);

            return _mapper.ToDetalleResponse(pedido);
        }

        public async Task<PedidoDetalleResponse> ObtenerPorIdAsync(Guid id, CancellationToken ct = default)
        {
            var pedido = await _pedidoRepository.ObtenerPorIdConDetalleAsync(id, ct)
                ?? throw new DomainException($"No se encontró el pedido con id '{id}'.", TipoErrorDominio.NoEncontrado);

            return _mapper.ToDetalleResponse(pedido);
        }

        private DateOnly ObtenerFechaPeru(DateTime utcNow)
        {
            TimeZoneInfo tz;
            try
            {
                tz = TimeZoneInfo.FindSystemTimeZoneById("America/Lima");
            }
            catch (TimeZoneNotFoundException)
            {
                tz = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
            }
            var fechaLima = TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz);

            return DateOnly.FromDateTime(fechaLima);
        }
    }
}
