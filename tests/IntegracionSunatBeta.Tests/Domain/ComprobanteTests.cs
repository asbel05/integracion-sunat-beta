using IntegracionSunatBeta.Api.Domain.Entities;

namespace IntegracionSunatBeta.Tests.Domain
{
    public class ComprobanteTests
    {
        [Theory]
        [InlineData("B001", 1, "B001-00000001")]
        [InlineData("F001", 123, "F001-00000123")]
        [InlineData("F001", 99999999, "F001-99999999")]
        public void NumeroCompleto_DebeFormatearSerieYCorrelativoConOchoDigitos(string serie, int correlativo, string esperado)
        {
            // Arrange
            var comprobante = new Comprobante
            {
                Serie = serie,
                Correlativo = correlativo
            };

            // Act
            var resultado = comprobante.NumeroCompleto;

            // Assert
            Assert.Equal(esperado, resultado);
        }
    }
}
