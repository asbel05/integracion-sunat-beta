using FluentValidation;
using IntegracionSunatBeta.Api.Application.DTOs;
using IntegracionSunatBeta.Api.Application.Interfaces;
using IntegracionSunatBeta.Api.Application.Mappers;
using IntegracionSunatBeta.Api.Application.Services;
using IntegracionSunatBeta.Api.Application.Validators;
using IntegracionSunatBeta.Api.Domain.Entities;
using IntegracionSunatBeta.Api.Domain.Enums;
using IntegracionSunatBeta.Api.Domain.Exceptions;
using NSubstitute;

namespace IntegracionSunatBeta.Tests.Services
{
    public class PedidoServiceTests
    {
        private readonly IPedidoRepository _pedidoRepository = Substitute.For<IPedidoRepository>();
        private readonly PedidoService _sut;

        public PedidoServiceTests()
        {
            _sut = new PedidoService(
                _pedidoRepository,
                new CrearPedidoRequestValidator(),
                new PedidoMapper());
        }

        [Fact]
        public async Task CrearAsync_BoletaValida_DebeCalcularIgvAwayFromZeroYGuardarEnSinEnviar()
        {
            // Arrange
            _pedidoRepository.ObtenerSiguienteCodigoAsync(Arg.Any<CancellationToken>()).Returns(7);
            _pedidoRepository.ObtenerSiguienteCorrelativoAsync("B001", Arg.Any<CancellationToken>()).Returns(15);
            var request = new CrearPedidoRequest(
                "1",
                "12345678",
                "  Asbel Cristobal  ",
                TipoComprobante.Boleta,
                [new CrearPedidoDetalleRequest("  Cable HDMI  ", 1, 10.25m)]);
            // Act
            var response = await _sut.CrearAsync(request);
            // Assert
            Assert.Equal("PED-0007", response.Codigo);
            Assert.Equal("Asbel Cristobal", response.ClienteNombre);
            Assert.Equal(10.25m, response.Subtotal);
            Assert.Equal(1.85m, response.Igv); // 10.25 * 0.18 = 1.845 -> AwayFromZero = 1.85
            Assert.Equal(12.10m, response.Total);
            Assert.Equal(TipoComprobante.Boleta, response.Comprobante.Tipo);
            Assert.Equal("B001", response.Comprobante.Serie);
            Assert.Equal(15, response.Comprobante.Correlativo);
            Assert.Equal("B001-00000015", response.Comprobante.NumeroCompleto);
            Assert.Equal(EstadoComprobante.SinEnviar, response.Comprobante.Estado);

            await _pedidoRepository.Received(1).AgregarAsync(
                Arg.Is<Pedido>(p => p.Codigo == "PED-0007" && p.Detalles.Count == 1),
                Arg.Any<CancellationToken>());

            await _pedidoRepository.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CrearAsync_FacturaValida_DebeUsarSerieF001()
        {
            // Arrange
            _pedidoRepository.ObtenerSiguienteCodigoAsync(Arg.Any<CancellationToken>()).Returns(1);
            _pedidoRepository.ObtenerSiguienteCorrelativoAsync("F001", Arg.Any<CancellationToken>()).Returns(3);
            var request = new CrearPedidoRequest(
                "6",
                "20100070970",
                "Empresa Demo SAC",
                TipoComprobante.Factura,
                [new CrearPedidoDetalleRequest("Soporte TI", 2, 50.00m)]);
            // Act
            var response = await _sut.CrearAsync(request);
            // Assert
            Assert.Equal("F001", response.Comprobante.Serie);
            Assert.Equal("F001-00000003", response.Comprobante.NumeroCompleto);
            Assert.Equal(100.00m, response.Subtotal);
            Assert.Equal(18.00m, response.Igv);
            Assert.Equal(118.00m, response.Total);
        }

        [Fact]
        public async Task CrearAsync_RequestInvalido_DebeLanzarValidationExceptionSinTocarSecuenciasNiBd()
        {
            // Arrange
            var request = new CrearPedidoRequest(
                "1",
                "12345678",
                "Cliente Test",
                TipoComprobante.Factura, // ilegal: Factura con DNI
                [new CrearPedidoDetalleRequest("Item", 1, 10m)]);

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => _sut.CrearAsync(request));
            await _pedidoRepository.DidNotReceive().ObtenerSiguienteCodigoAsync(Arg.Any<CancellationToken>());
            await _pedidoRepository.DidNotReceive().AgregarAsync(Arg.Any<Pedido>(), Arg.Any<CancellationToken>());
            await _pedidoRepository.DidNotReceive().GuardarCambiosAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ObtenerPorIdAsync_CuandoNoExiste_DebeLanzarDomainExceptionNoEncontrado()
        {
            // Arrange
            var idInexistente = Guid.NewGuid();
            _pedidoRepository.ObtenerPorIdConDetalleAsync(idInexistente, Arg.Any<CancellationToken>())
                .Returns((Pedido?)null);
            // Act
            var ex = await Assert.ThrowsAsync<DomainException>(() => _sut.ObtenerPorIdAsync(idInexistente));
            // Assert
            Assert.Equal(TipoErrorDominio.NoEncontrado, ex.Tipo);
        }
    }
}