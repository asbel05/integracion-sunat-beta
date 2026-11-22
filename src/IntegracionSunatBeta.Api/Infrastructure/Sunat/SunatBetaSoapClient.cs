using IntegracionSunatBeta.Api.Application.Interfaces;
using IntegracionSunatBeta.Api.Domain.Entities;
using Microsoft.Extensions.Options;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;

namespace IntegracionSunatBeta.Api.Infrastructure.Sunat
{
    public class SunatBetaSoapClient : ISunatClient
    {
        private static readonly XNamespace NsSoapEnv = "http://schemas.xmlsoap.org/soap/envelope/";
        private static readonly XNamespace NsSer = "http://service.sunat.gob.pe";
        private static readonly XNamespace NsWsse = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
        private readonly HttpClient _httpClient;
        private readonly SunatOptions _options;
        private readonly UblInvoiceXmlBuilder _xmlBuilder;
        private readonly SunatXmlSigner _xmlSigner;
        private readonly ILogger<SunatBetaSoapClient> _logger;

        public SunatBetaSoapClient(
            HttpClient httpClient,
            IOptions<SunatOptions> options,
            UblInvoiceXmlBuilder xmlBuilder,
            SunatXmlSigner xmlSigner,
            ILogger<SunatBetaSoapClient> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _xmlBuilder = xmlBuilder;
            _xmlSigner = xmlSigner;
            _logger = logger;
        }

        public async Task<SunatEnvioResultado> EnviarComprobanteAsync(Comprobante comprobante, CancellationToken ct = default)
        {
            if (_options.SimularErrorTransitorio)
            {
                _logger.LogWarning("Simulando error transitorio de SUNAT para comprobante {Numero}.", comprobante.NumeroCompleto);
                return new SunatEnvioResultado(
                    TipoResultadoSunat.ErrorTransitorio,
                    "SIM-503",
                    "Error transitorio simulado (Sunat:SimularErrorTransitorio = true).");
            }

            if (string.IsNullOrWhiteSpace(_options.UsuarioSol) || string.IsNullOrWhiteSpace(_options.ClaveSol))
                throw new InvalidOperationException("No se configuraron 'Sunat:UsuarioSol' y 'Sunat:ClaveSol'.");

            var nombreBase = _xmlBuilder.ConstruirNombreBaseArchivo(comprobante);
            var xmlDoc = _xmlBuilder.ConstruirXml(comprobante);
            var xmlFirmadoBytes = _xmlSigner.FirmarXml(xmlDoc);
            var zipBase64 = await ComprimirEnZipBase64Async($"{nombreBase}.xml", xmlFirmadoBytes, ct);
            var soapXml = ConstruirSobreSoap($"{nombreBase}.zip", zipBase64);
            using var request = new HttpRequestMessage(HttpMethod.Post, _options.EndpointUrl);

            request.Content = new StringContent(soapXml, new UTF8Encoding(false), "text/xml");
            request.Headers.ConnectionClose = true;
            request.Headers.Add("SOAPAction", "\"urn:sendBill\"");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));

            try
            {
                using var response = await _httpClient.SendAsync(request, ct);
                var responseBody = await response.Content.ReadAsStringAsync(ct);

                return InterpretarRespuestaSoap(responseBody, (int)response.StatusCode);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Error de red enviando comprobante {Numero} a SUNAT.", comprobante.NumeroCompleto);

                return new SunatEnvioResultado(
                    TipoResultadoSunat.ErrorTransitorio,
                    "NET-ERR",
                    Recortar($"Error de red al conectar con SUNAT: {ex.Message}", 500));
            }
            catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Timeout enviando comprobante {Numero} a SUNAT.", comprobante.NumeroCompleto);

                return new SunatEnvioResultado(
                    TipoResultadoSunat.ErrorTransitorio,
                    "TIMEOUT",
                    "Tiempo de espera agotado al conectar con SUNAT Beta.");
            }
        }

        public SunatEnvioResultado InterpretarRespuestaSoap(string responseBody, int httpStatusCode)
        {
            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return new SunatEnvioResultado(
                    TipoResultadoSunat.ErrorTransitorio,
                    $"HTTP-{httpStatusCode}",
                    $"SUNAT respondió vacío con HTTP {httpStatusCode}.");
            }
            XDocument soapDoc;

            try
            {
                soapDoc = XDocument.Parse(responseBody);
            }
            catch
            {
                return new SunatEnvioResultado(
                    TipoResultadoSunat.ErrorTransitorio,
                    $"HTTP-{httpStatusCode}",
                    Recortar($"Respuesta no XML de SUNAT (HTTP {httpStatusCode}): {responseBody}", 500));
            }
            var faultNode = soapDoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Fault");

            if (faultNode is not null)
            {
                var faultCode = faultNode.Descendants().FirstOrDefault(e => e.Name.LocalName == "faultcode")?.Value?.Trim() ?? "FAULT";
                var faultString = faultNode.Descendants().FirstOrDefault(e => e.Name.LocalName == "faultstring")?.Value?.Trim() ?? "Error SOAP en SUNAT.";
                var detailMessage = faultNode.Descendants().FirstOrDefault(e => e.Name.LocalName == "message")?.Value?.Trim();
                var mensajeCompleto = string.IsNullOrWhiteSpace(detailMessage)
                    ? faultString
                    : $"{faultString} - {detailMessage}";
                var esErrorCliente = faultCode.Contains("Client", StringComparison.OrdinalIgnoreCase);

                return new SunatEnvioResultado(
                    esErrorCliente ? TipoResultadoSunat.Rechazado : TipoResultadoSunat.ErrorTransitorio,
                    Recortar(faultCode, 50),
                    Recortar(mensajeCompleto, 500));
            }
            var appResponseBase64 = soapDoc.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "applicationResponse")
                ?.Value?.Trim();

            if (string.IsNullOrWhiteSpace(appResponseBase64))
            {
                return new SunatEnvioResultado(
                    TipoResultadoSunat.ErrorTransitorio,
                    $"HTTP-{httpStatusCode}",
                    $"La respuesta SOAP de SUNAT no contiene applicationResponse (HTTP {httpStatusCode}).");
            }

            return LeerCdrDesdeZipBase64(appResponseBase64);
        }

        private SunatEnvioResultado LeerCdrDesdeZipBase64(string cdrZipBase64)
        {
            var zipBytes = Convert.FromBase64String(cdrZipBase64);
            using var ms = new MemoryStream(zipBytes);
            using var archive = new ZipArchive(ms, ZipArchiveMode.Read);
            var xmlEntry = archive.Entries.FirstOrDefault(e =>
                e.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("El archivo ZIP del CDR de SUNAT no contiene ningún XML.");
            using var entryStream = xmlEntry.Open();
            var cdrDoc = XDocument.Load(entryStream);
            var responseCode = cdrDoc.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "ResponseCode")
                ?.Value?.Trim() ?? string.Empty;
            var description = cdrDoc.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "Description")
                ?.Value?.Trim() ?? "Sin descripción en CDR.";
            var tipo = responseCode == "0"
                ? TipoResultadoSunat.Aceptado
                : TipoResultadoSunat.Rechazado;

            return new SunatEnvioResultado(
                tipo,
                Recortar(responseCode, 50),
                Recortar(description, 500));
        }

        private string ConstruirSobreSoap(string nombreZip, string zipBase64)
        {
            var envelope = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(NsSoapEnv + "Envelope",
                    new XAttribute(XNamespace.Xmlns + "soapenv", NsSoapEnv),
                    new XAttribute(XNamespace.Xmlns + "ser", NsSer),
                    new XAttribute(XNamespace.Xmlns + "wsse", NsWsse),
                    new XElement(NsSoapEnv + "Header",
                        new XElement(NsWsse + "Security",
                            new XElement(NsWsse + "UsernameToken",
                                new XElement(NsWsse + "Username", _options.UsuarioSol),
                                new XElement(NsWsse + "Password", _options.ClaveSol)))),
                    new XElement(NsSoapEnv + "Body",
                        new XElement(NsSer + "sendBill",
                            new XElement("fileName", nombreZip),
                            new XElement("contentFile", zipBase64)))));

            return envelope.ToString(SaveOptions.DisableFormatting);
        }

        private static async Task<string> ComprimirEnZipBase64Async(string nombreXml, byte[] xmlBytes, CancellationToken ct)
        {
            using var zipStream = new MemoryStream();
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                var entry = archive.CreateEntry(nombreXml, CompressionLevel.Optimal);
                await using var entryStream = entry.Open();
                await entryStream.WriteAsync(xmlBytes, ct);
            }

            return Convert.ToBase64String(zipStream.ToArray());
        }

        private static string Recortar(string texto, int maxLen)
        {
            return texto.Length <= maxLen ? texto : texto[..maxLen];
        }
    }
}
