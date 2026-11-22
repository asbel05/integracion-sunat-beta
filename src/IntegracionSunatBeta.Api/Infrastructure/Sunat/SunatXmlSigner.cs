using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace IntegracionSunatBeta.Api.Infrastructure.Sunat
{
    public class SunatXmlSigner
    {
        private static readonly UTF8Encoding Utf8SinBom = new(encoderShouldEmitUTF8Identifier: false);
        private readonly SunatOptions _options;

        public SunatXmlSigner(IOptions<SunatOptions> options)
        {
            _options = options.Value;
        }

        public byte[] FirmarXml(XDocument documento)
        {
            if (string.IsNullOrWhiteSpace(_options.CertificadoBase64))
                throw new InvalidOperationException("No se configuró 'Sunat:CertificadoBase64'.");

            var pfxBytes = Convert.FromBase64String(_options.CertificadoBase64);

            using var certificado = X509CertificateLoader.LoadPkcs12(
                pfxBytes,
                _options.CertificadoPassword,
                X509KeyStorageFlags.EphemeralKeySet);

            return FirmarConCertificado(documento, certificado);
        }

        public byte[] FirmarConCertificado(XDocument documento, X509Certificate2 certificado)
        {
            using var rsa = certificado.GetRSAPrivateKey()
                ?? throw new InvalidOperationException("El certificado no contiene una clave privada RSA.");
            var xmlDoc = new XmlDocument { PreserveWhitespace = true };

            using (var inputStream = new MemoryStream())
            {
                using (var writer = XmlWriter.Create(inputStream, new XmlWriterSettings
                {
                    Encoding = Utf8SinBom,
                    Indent = false,
                    OmitXmlDeclaration = false
                }))
                {
                    documento.Save(writer);
                }
                inputStream.Position = 0;
                xmlDoc.Load(inputStream);
            }
            var nsManager = new XmlNamespaceManager(xmlDoc.NameTable);
            nsManager.AddNamespace("ext", "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2");
            var extensionContent = xmlDoc.SelectSingleNode("//ext:ExtensionContent", nsManager)
                ?? throw new InvalidOperationException("El XML UBL no contiene el nodo <ext:ExtensionContent>.");
            var signedXml = new SignedXml(xmlDoc)
            {
                SigningKey = rsa
            };
            signedXml.Signature.Id = "SignSUNAT";
            signedXml.SignedInfo!.SignatureMethod = SignedXml.XmlDsigRSASHA256Url;
            var reference = new Reference(string.Empty)
            {
                DigestMethod = SignedXml.XmlDsigSHA256Url
            };
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            signedXml.AddReference(reference);

            var keyInfo = new KeyInfo();
            keyInfo.AddClause(new KeyInfoX509Data(certificado));
            signedXml.KeyInfo = keyInfo;
            signedXml.ComputeSignature();

            var firmaElement = signedXml.GetXml();
            extensionContent.AppendChild(xmlDoc.ImportNode(firmaElement, deep: true));

            using var outputStream = new MemoryStream();

            using (var writer = XmlWriter.Create(outputStream, new XmlWriterSettings
            {
                Encoding = Utf8SinBom,
                Indent = false,
                OmitXmlDeclaration = false
            }))
            {
                xmlDoc.Save(writer);
            }

            return outputStream.ToArray();
        }
    }
}
