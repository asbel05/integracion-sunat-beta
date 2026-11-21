using IntegracionSunatBeta.Api.Application.Interfaces;
using IntegracionSunatBeta.Api.Application.Mappers;
using IntegracionSunatBeta.Api.Application.Services;
using IntegracionSunatBeta.Api.Domain.Entities;
using IntegracionSunatBeta.Api.Domain.Enums;
using IntegracionSunatBeta.Api.Domain.Exceptions;
using NSubstitute;

namespace IntegracionSunatBeta.Tests.Services
{
    public class ComprobanteServiceTests
    {
        private readonly IComprobanteRepository _comprobanteRepository = Substitute.For<IComprobanteRepository>();
        private readonly ComprobanteService _sut;

        public ComprobanteServiceTests()
        {
            _sut = new ComprobanteService(_comprobanteRepository, new ComprobanteMapper());
        }

        [Fact]
        public async Task ListarAsync_ConParametrosFueraDeRango_DebeAcotarPaginaYTamanioYCalcularTotalPages()
        {
            // Arrange
            var comprobante = new Comprobante
            {
                Id = Guid.NewGuid(),
                PedidoId = Guid.NewGuid(),
                Tipo = TipoComprobante.Boleta,
                Serie = "B001",
                Correlativo = 1,
                FechaEmision = new DateOnly(2026, 11, 21),
                Estado = EstadoComprobante.SinEnviar,
                Pedido = new Pedido
                {
                    Codigo = "PED-0001",
                    ClienteTipoDoc = "1",
                    ClienteNumDoc = "12345678",
                    ClienteNombre = "Asbel",
                    Total = 118.00m
                }
            };

            _comprobanteRepository.ListarAsync(
                    null, null, 1, 100, Arg.Any<CancellationToken>())
                .Returns(([comprobante], 205));

            // Act
            var response = await _sut.ListarAsync(null, null, page: 0, pageSize: 500);

            // Assert
            Assert.Equal(1, response.Page);
            Assert.Equal(100, response.PageSize);
            Assert.Equal(205, response.TotalCount);
            Assert.Equal(3, response.TotalPages); // 205 / 100 = 2.05 -> Ceiling = 3
            Assert.Single(response.Items);
            Assert.Equal("B001-00000001", response.Items[0].NumeroCompleto);
        }

        [Fact]
        public async Task ObtenerPorIdAsync_CuandoNoExiste_DebeLanzarDomainExceptionNoEncontrado()
        {
            // Arrange
            var idInexistente = Guid.NewGuid();
            _comprobanteRepository.ObtenerPorIdConDetallesIntentosAsync(idInexistente, Arg.Any<CancellationToken>())
                .Returns((Comprobante?)null);

            // Act
            var ex = await Assert.ThrowsAsync<DomainException>(() => _sut.ObtenerPorIdAsync(idInexistente));

            // Assert
            Assert.Equal(TipoErrorDominio.NoEncontrado, ex.Tipo);
        }
    }
}