using IntegracionSunatBeta.Api.Domain.Entities;
using IntegracionSunatBeta.Api.Domain.Enums;
using IntegracionSunatBeta.Api.Infrastructure.Sunat;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using System.Xml.Linq;

namespace IntegracionSunatBeta.Tests.Sunat
{
    public class UblAndSignerTests
    {
        private readonly IOptions<SunatOptions> _options = Options.Create(new SunatOptions
        {
            RucEmisor = "20000000001",
            RazonSocialEmisor = "EMPRESA DEMO SUNAT BETA SAC",
            UbigeoEmisor = "150101"
        });

        private Comprobante CrearComprobantePrueba(TipoComprobante tipo, string serie, int correlativo, 
            string clienteTipoDoc, string clienteNumDoc)
        {
            var pedido = new Pedido
            {
                Id = Guid.NewGuid(),
                Codigo = "PED-0001",
                ClienteTipoDoc = clienteTipoDoc,
                ClienteNumDoc = clienteNumDoc,
                ClienteNombre = "Cliente Demo",
                Subtotal = 100.00m,
                Igv = 18.00m,
                Total = 118.00m,
                Detalles =
                [
                    new PedidoDetalle
                    {
                        Id = Guid.NewGuid(),
                        Descripcion = "Servicio de Prueba",
                        Cantidad = 1,
                        PrecioUnitario = 100.00m,
                        Subtotal = 100.00m
                    }
                ]
            };
            return new Comprobante
            {
                Id = Guid.NewGuid(),
                PedidoId = pedido.Id,
                Tipo = tipo,
                Serie = serie,
                Correlativo = correlativo,
                FechaEmision = new DateOnly(2026, 11, 21),
                Estado = EstadoComprobante.SinEnviar,
                Pedido = pedido
            };
        }

        [Fact]
        public void ConstruirXml_Factura_DebeIncluirPaymentTermsContadoYMontosConPuntoDecimal()
        {
            // Arrange
            var builder = new UblInvoiceXmlBuilder(_options);
            var comprobante = CrearComprobantePrueba(TipoComprobante.Factura, "F001", 1, "6", "20100070970");
            // Act
            var nombreBase = builder.ConstruirNombreBaseArchivo(comprobante);
            var xml = builder.ConstruirXml(comprobante);
            // Assert
            Assert.Equal("20000000001-01-F001-00000001", nombreBase);
            XNamespace cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
            XNamespace cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
            Assert.Equal("2.1", xml.Root!.Element(cbc + "UBLVersionID")?.Value);
            Assert.Equal("01", xml.Root.Element(cbc + "InvoiceTypeCode")?.Value);
            Assert.Equal("Contado", xml.Root.Element(cac + "PaymentTerms")?.Element(cbc + "PaymentMeansID")?.Value);
            Assert.Equal("118.00", xml.Root.Element(cac + "LegalMonetaryTotal")?.Element(cbc + "PayableAmount")?.Value);
        }

        [Fact]
        public void ConstruirXml_Boleta_NoDebeIncluirPaymentTerms()
        {
            // Arrange
            var builder = new UblInvoiceXmlBuilder(_options);
            var comprobante = CrearComprobantePrueba(TipoComprobante.Boleta, "B001", 5, "1", "12345678");
            // Act
            var nombreBase = builder.ConstruirNombreBaseArchivo(comprobante);
            var xml = builder.ConstruirXml(comprobante);
            // Assert
            Assert.Equal("20000000001-03-B001-00000005", nombreBase);
            XNamespace cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
            XNamespace cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
            Assert.Equal("03", xml.Root!.Element(cbc + "InvoiceTypeCode")?.Value);
            Assert.Null(xml.Root.Element(cac + "PaymentTerms"));
        }

        [Fact]
        public void FirmarConCertificado_DebeGenerarUtf8SinBomYFirmaValidaEnExtensionContent()
        {
            // Arrange
            var builder = new UblInvoiceXmlBuilder(_options);
            var signer = new SunatXmlSigner(_options);
            var comprobante = CrearComprobantePrueba(TipoComprobante.Factura, "F001", 1, "6", "20100070970");
            var xml = builder.ConstruirXml(comprobante);
            using var rsa = RSA.Create(2048);
            var certReq = new CertificateRequest("CN=TEST SUNAT", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            using var cert = certReq.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
            // Act
            var bytesFirmados = signer.FirmarConCertificado(xml, cert);
            // Assert: verificar UTF-8 sin BOM (EF BB BF)
            Assert.False(bytesFirmados.Length >= 3 && bytesFirmados[0] == 0xEF && bytesFirmados[1] == 0xBB && bytesFirmados[2] == 0xBF);
            // Assert: verificar firma criptográfica válida
            var docFirmado = new XmlDocument { PreserveWhitespace = true };
            using (var ms = new MemoryStream(bytesFirmados))
            {
                docFirmado.Load(ms);
            }
            var nsManager = new XmlNamespaceManager(docFirmado.NameTable);
            nsManager.AddNamespace("ds", SignedXml.XmlDsigNamespaceUrl);
            var signatureNode = docFirmado.SelectSingleNode("//ds:Signature", nsManager) as XmlElement;
            Assert.NotNull(signatureNode);
            var verifier = new SignedXml(docFirmado);
            verifier.LoadXml(signatureNode!);
            Assert.True(verifier.CheckSignature(cert, verifySignatureOnly: true));
        }
    }
}
