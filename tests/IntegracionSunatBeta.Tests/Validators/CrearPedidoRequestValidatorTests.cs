using IntegracionSunatBeta.Api.Application.DTOs;
using IntegracionSunatBeta.Api.Application.Validators;
using IntegracionSunatBeta.Api.Domain.Enums;

namespace IntegracionSunatBeta.Tests.Validators
{
    public class CrearPedidoRequestValidatorTests
    {
        private readonly CrearPedidoRequestValidator _validator = new();

        private static CrearPedidoRequest CrearRequestValido(
            string clienteTipoDoc = "1",
            string clienteNumDoc = "12345678",
            string clienteNombre = "Asbel Cristobal",
            TipoComprobante tipoComprobante = TipoComprobante.Boleta,
            List<CrearPedidoDetalleRequest>? detalles = null)
        {
            return new CrearPedidoRequest(
                clienteTipoDoc,
                clienteNumDoc,
                clienteNombre,
                tipoComprobante,
                detalles ?? [new CrearPedidoDetalleRequest("Producto Demo", 1, 100.00m)]);
        }

        [Fact]
        public void Validate_BoletaConDniValido_DebeSerValido()
        {
            // Arrange
            var request = CrearRequestValido(
                clienteTipoDoc: "1",
                clienteNumDoc: "12345678",
                tipoComprobante: TipoComprobante.Boleta);
            // Act
            var result = _validator.Validate(request);
            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_FacturaConRucValido_DebeSerValido()
        {
            // Arrange
            var request = CrearRequestValido(
                clienteTipoDoc: "6",
                clienteNumDoc: "20100070970",
                tipoComprobante: TipoComprobante.Factura);
            // Act
            var result = _validator.Validate(request);
            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_FacturaConDni_DebeFallarEnClienteTipoDoc()
        {
            // Arrange
            var request = CrearRequestValido(
                clienteTipoDoc: "1",
                clienteNumDoc: "12345678",
                tipoComprobante: TipoComprobante.Factura);
            // Act
            var result = _validator.Validate(request);
            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(CrearPedidoRequest.ClienteTipoDoc));
        }

        [Theory]
        [InlineData("1234567")]   // 7 dígitos
        [InlineData("123456789")] // 9 dígitos
        [InlineData("1234567A")]  // contiene letra
        [InlineData("")]          // vacío
        public void Validate_BoletaConDniInvalido_DebeFallarEnClienteNumDoc(string dniInvalido)
        {
            // Arrange
            var request = CrearRequestValido(
                clienteTipoDoc: "1",
                clienteNumDoc: dniInvalido,
                tipoComprobante: TipoComprobante.Boleta);
            // Act
            var result = _validator.Validate(request);
            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(CrearPedidoRequest.ClienteNumDoc));
        }

        [Theory]
        [InlineData("99123456789")] // prefijo inválido
        [InlineData("2012345678")]  // 10 dígitos
        [InlineData("201234567890")] // 12 dígitos
        [InlineData("2012345678A")] // contiene letra
        public void Validate_FacturaConRucInvalido_DebeFallarEnClienteNumDoc(string rucInvalido)
        {
            // Arrange
            var request = CrearRequestValido(
                clienteTipoDoc: "6",
                clienteNumDoc: rucInvalido,
                tipoComprobante: TipoComprobante.Factura);
            // Act
            var result = _validator.Validate(request);
            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(CrearPedidoRequest.ClienteNumDoc));
        }

        [Fact]
        public void Validate_SinDetalles_DebeFallarEnDetalles()
        {
            // Arrange
            var request = CrearRequestValido(detalles: []);
            // Act
            var result = _validator.Validate(request);
            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(CrearPedidoRequest.Detalles));
        }

        [Fact]
        public void Validate_MasDeCienDetalles_DebeFallarEnDetalles()
        {
            // Arrange
            var muchosItems = Enumerable.Range(1, 101)
                .Select(i => new CrearPedidoDetalleRequest($"Item {i}", 1, 10m))
                .ToList();
            var request = CrearRequestValido(detalles: muchosItems);
            // Act
            var result = _validator.Validate(request);
            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(CrearPedidoRequest.Detalles));
        }

        [Theory]
        [InlineData(0, 10.00)]       // cantidad 0
        [InlineData(-2, 10.00)]      // cantidad negativa
        [InlineData(1, 0.00)]        // precio 0
        [InlineData(1, -5.00)]       // precio negativo
        [InlineData(1, 10.125)]      // más de 2 decimales
        public void Validate_DetalleConCantidadOPrecioInvalido_DebeFallar(int cantidad, decimal precio)
        {
            // Arrange
            var request = CrearRequestValido(detalles:
            [
                new CrearPedidoDetalleRequest("Mouse", cantidad, precio)
            ]);
            // Act
            var result = _validator.Validate(request);
            // Assert
            Assert.False(result.IsValid);
        }
    }
}
