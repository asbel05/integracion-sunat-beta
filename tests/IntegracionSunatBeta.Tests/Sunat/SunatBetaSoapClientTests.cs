using IntegracionSunatBeta.Api.Application.Interfaces;
using IntegracionSunatBeta.Api.Domain.Entities;
using IntegracionSunatBeta.Api.Domain.Enums;
using IntegracionSunatBeta.Api.Infrastructure.Sunat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.IO.Compression;
using System.Text;
using Xunit.Abstractions;

namespace IntegracionSunatBeta.Tests.Sunat
{
    public class SunatBetaSoapClientTests
    {
        private readonly ITestOutputHelper _output;

        public SunatBetaSoapClientTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task EnviarComprobanteAsync_ConSimularErrorTransitorio_DebeDevolverErrorTransitorio()
        {
            // Arrange
            var opts = Options.Create(new SunatOptions { SimularErrorTransitorio = true });
            var client = CrearCliente(opts);
            var comprobante = CrearComprobante(TipoComprobante.Boleta, "B001", 1, "1", "12345678");
            // Act
            var resultado = await client.EnviarComprobanteAsync(comprobante);
            // Assert
            Assert.Equal(TipoResultadoSunat.ErrorTransitorio, resultado.Tipo);
            Assert.Equal("SIM-503", resultado.CodigoCdr);
        }

        [Fact]
        public void InterpretarRespuestaSoap_CuandoCdrEsCeroConCarpetaDummy_DebeDevolverAceptado()
        {
            // Arrange
            var client = CrearCliente(Options.Create(new SunatOptions()));
            var cdrXml = """
                <?xml version="1.0" encoding="utf-8"?>
                <ar:ApplicationResponse xmlns:ar="urn:oasis:names:specification:ubl:schema:xsd:ApplicationResponse-2"
                                        xmlns:cac="urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2"
                                        xmlns:cbc="urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2">
                  <cac:DocumentResponse>
                    <cac:Response>
                      <cbc:ResponseCode>0</cbc:ResponseCode>
                      <cbc:Description>La Factura numero F001-00000001, ha sido aceptada</cbc:Description>
                    </cac:Response>
                  </cac:DocumentResponse>
                </ar:ApplicationResponse>
                """;
            using var ms = new MemoryStream();
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                zip.CreateEntry("dummy/");
                var entry = zip.CreateEntry("R-20000000001-01-F001-00000001.xml");
                using var sw = new StreamWriter(entry.Open(), new UTF8Encoding(false));
                sw.Write(cdrXml);
            }
            var zipBase64 = Convert.ToBase64String(ms.ToArray());
            var soapResponse = $"""
                <soap-env:Envelope xmlns:soap-env="http://schemas.xmlsoap.org/soap/envelope/" xmlns:br="http://service.sunat.gob.pe">
                  <soap-env:Body>
                    <br:sendBillResponse>
                      <applicationResponse>{zipBase64}</applicationResponse>
                    </br:sendBillResponse>
                  </soap-env:Body>
                </soap-env:Envelope>
                """;
            // Act
            var resultado = client.InterpretarRespuestaSoap(soapResponse, 200);
            // Assert
            Assert.Equal(TipoResultadoSunat.Aceptado, resultado.Tipo);
            Assert.Equal("0", resultado.CodigoCdr);
            Assert.Contains("aceptada", resultado.Mensaje, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void InterpretarRespuestaSoap_CuandoFaultEsClient_DebeDevolverRechazado()
        {
            // Arrange
            var client = CrearCliente(Options.Create(new SunatOptions()));
            var soapFault = """
                <soap-env:Envelope xmlns:soap-env="http://schemas.xmlsoap.org/soap/envelope/">
                  <soap-env:Body>
                    <soap-env:Fault>
                      <faultcode>soap-env:Client.2017</faultcode>
                      <faultstring>El numero de documento del receptor no es valido</faultstring>
                    </soap-env:Fault>
                  </soap-env:Body>
                </soap-env:Envelope>
                """;
            // Act
            var resultado = client.InterpretarRespuestaSoap(soapFault, 500);
            // Assert
            Assert.Equal(TipoResultadoSunat.Rechazado, resultado.Tipo);
            Assert.Equal("soap-env:Client.2017", resultado.CodigoCdr);
        }

        [Fact]
        public void InterpretarRespuestaSoap_CuandoFaultEsServer_DebeDevolverErrorTransitorio()
        {
            // Arrange
            var client = CrearCliente(Options.Create(new SunatOptions()));
            var soapFault = """
                <soap-env:Envelope xmlns:soap-env="http://schemas.xmlsoap.org/soap/envelope/">
                  <soap-env:Body>
                    <soap-env:Fault>
                      <faultcode>soap-env:Server.0100</faultcode>
                      <faultstring>El sistema no puede responder su solicitud</faultstring>
                    </soap-env:Fault>
                  </soap-env:Body>
                </soap-env:Envelope>
                """;
            // Act
            var resultado = client.InterpretarRespuestaSoap(soapFault, 500);
            // Assert
            Assert.Equal(TipoResultadoSunat.ErrorTransitorio, resultado.Tipo);
        }

        [Fact(Skip = "Prueba de integración en vivo contra e-beta.sunat.gob.pe (requiere internet y user-secrets).")]
        public async Task EnviarCincoBoletasYCincoFacturas_ContraSunatBetaReal()
        {
            // Arrange: lee tus User Secrets reales del proyecto Api
            var config = new ConfigurationBuilder()
                .AddUserSecrets("ff675e52-9d49-4e27-994f-1aed50022b53")
                .Build();
            var sunatOptions = new SunatOptions();
            config.GetSection(SunatOptions.SectionName).Bind(sunatOptions);
            Assert.False(string.IsNullOrWhiteSpace(sunatOptions.CertificadoBase64), "Falta Sunat:CertificadoBase64 en user-secrets.");
            var client = CrearCliente(Options.Create(sunatOptions));
            // Act & Assert: 5 Boletas reales contra e-beta.sunat.gob.pe
            for (var i = 1; i <= 5; i++)
            {
                var boleta = CrearComprobante(TipoComprobante.Boleta, "B001", i, "1", "12345678");
                var res = await client.EnviarComprobanteAsync(boleta);
                _output.WriteLine($"[BOLETA {boleta.NumeroCompleto}] Tipo={res.Tipo} | CDR={res.CodigoCdr} | Msg={res.Mensaje}");
                Assert.Equal(TipoResultadoSunat.Aceptado, res.Tipo);
                await Task.Delay(300);
            }
            // Act & Assert: 5 Facturas reales contra e-beta.sunat.gob.pe
            for (var i = 1; i <= 5; i++)
            {
                var factura = CrearComprobante(TipoComprobante.Factura, "F001", i, "6", "20100070970");
                var res = await client.EnviarComprobanteAsync(factura);
                _output.WriteLine($"[FACTURA {factura.NumeroCompleto}] Tipo={res.Tipo} | CDR={res.CodigoCdr} | Msg={res.Mensaje}");
                Assert.Equal(TipoResultadoSunat.Aceptado, res.Tipo);
                await Task.Delay(300);
            }
        }

        private static SunatBetaSoapClient CrearCliente(IOptions<SunatOptions> options)
        {
            var httpClient = new HttpClient(new SocketsHttpHandler { UseCookies = false })
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            return new SunatBetaSoapClient(
                httpClient,
                options,
                new UblInvoiceXmlBuilder(options),
                new SunatXmlSigner(options),
                NullLogger<SunatBetaSoapClient>.Instance);
        }

        private static Comprobante CrearComprobante(TipoComprobante tipo, string serie, int correlativo,
            string clienteTipoDoc, string clienteNumDoc)
        {
            TimeZoneInfo tz;
            try { 
                tz = TimeZoneInfo.FindSystemTimeZoneById("America/Lima"); 
            }
            catch (TimeZoneNotFoundException) { 
                tz = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time"); 
            }
            var fechaPeru = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz));
            var pedido = new Pedido
            {
                Id = Guid.NewGuid(),
                Codigo = $"PED-{correlativo:D4}",
                ClienteTipoDoc = clienteTipoDoc,
                ClienteNumDoc = clienteNumDoc,
                ClienteNombre = tipo == TipoComprobante.Factura ? "EMPRESA CLIENTE DEMO SAC" : "ASBEL CRISTOBAL DEMO",
                Subtotal = 100.00m,
                Igv = 18.00m,
                Total = 118.00m,
                Detalles =
                [
                    new PedidoDetalle
                    {
                        Id = Guid.NewGuid(),
                        Descripcion = "Servicio Demo Integracion SUNAT",
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
                FechaEmision = fechaPeru,
                Estado = EstadoComprobante.SinEnviar,
                Pedido = pedido
            };
        }
    }
}
