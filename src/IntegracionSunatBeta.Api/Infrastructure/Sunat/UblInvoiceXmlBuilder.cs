using IntegracionSunatBeta.Api.Domain.Entities;
using IntegracionSunatBeta.Api.Domain.Enums;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Xml.Linq;

namespace IntegracionSunatBeta.Api.Infrastructure.Sunat
{
    public class UblInvoiceXmlBuilder
    {
        private static readonly XNamespace NsInvoice = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
        private static readonly XNamespace NsCac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
        private static readonly XNamespace NsCbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
        private static readonly XNamespace NsExt = "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2";
        private static readonly XNamespace NsDs = "http://www.w3.org/2000/09/xmldsig#";
        private readonly SunatOptions _options;

        public UblInvoiceXmlBuilder(IOptions<SunatOptions> options)
        {
            _options = options.Value;
        }

        public string ConstruirNombreBaseArchivo(Comprobante comprobante)
        {
            var tipoCodigo = ObtenerCodigoTipo(comprobante.Tipo);
            return $"{_options.RucEmisor}-{tipoCodigo}-{comprobante.NumeroCompleto}";
        }

        public XDocument ConstruirXml(Comprobante comprobante)
        {
            var pedido = comprobante.Pedido
                ?? throw new InvalidOperationException($"El comprobante {comprobante.Id} no tiene pedido cargado.");
            var tipoCodigo = ObtenerCodigoTipo(comprobante.Tipo);
            var invoice = new XElement(NsInvoice + "Invoice",
                new XAttribute(XNamespace.Xmlns + "cac", NsCac),
                new XAttribute(XNamespace.Xmlns + "cbc", NsCbc),
                new XAttribute(XNamespace.Xmlns + "ext", NsExt),
                new XAttribute(XNamespace.Xmlns + "ds", NsDs),
                new XElement(NsExt + "UBLExtensions",
                    new XElement(NsExt + "UBLExtension",
                        new XElement(NsExt + "ExtensionContent"))),
                new XElement(NsCbc + "UBLVersionID", "2.1"),
                new XElement(NsCbc + "CustomizationID", "2.0"),
                new XElement(NsCbc + "ID", comprobante.NumeroCompleto),
                new XElement(NsCbc + "IssueDate", comprobante.FechaEmision.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                new XElement(NsCbc + "InvoiceTypeCode",
                    new XAttribute("listID", "0101"),
                    new XAttribute("listAgencyName", "PE:SUNAT"),
                    new XAttribute("listName", "Tipo de Documento"),
                    new XAttribute("listURI", "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo01"),
                    tipoCodigo),
                new XElement(NsCbc + "DocumentCurrencyCode",
                    new XAttribute("listID", "ISO 4217 Alpha"),
                    new XAttribute("listName", "Currency"),
                    new XAttribute("listAgencyName", "United Nations Economic Commission for Europe"),
                    "PEN"),
                new XElement(NsCac + "Signature",
                    new XElement(NsCbc + "ID", "SignSUNAT"),
                    new XElement(NsCac + "SignatoryParty",
                        new XElement(NsCac + "PartyIdentification",
                            new XElement(NsCbc + "ID", _options.RucEmisor)),
                        new XElement(NsCac + "PartyName",
                            new XElement(NsCbc + "Name", _options.RazonSocialEmisor))),
                    new XElement(NsCac + "DigitalSignatureAttachment",
                        new XElement(NsCac + "ExternalReference",
                            new XElement(NsCbc + "URI", "#SignSUNAT")))),
                new XElement(NsCac + "AccountingSupplierParty",
                    new XElement(NsCac + "Party",
                        new XElement(NsCac + "PartyIdentification",
                            new XElement(NsCbc + "ID",
                                new XAttribute("schemeID", "6"),
                                new XAttribute("schemeName", "Documento de Identidad"),
                                new XAttribute("schemeAgencyName", "PE:SUNAT"),
                                new XAttribute("schemeURI", "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo06"),
                                _options.RucEmisor)),
                        new XElement(NsCac + "PartyName",
                            new XElement(NsCbc + "Name", _options.RazonSocialEmisor)),
                        new XElement(NsCac + "PartyLegalEntity",
                            new XElement(NsCbc + "RegistrationName", _options.RazonSocialEmisor),
                            new XElement(NsCac + "RegistrationAddress",
                                new XElement(NsCbc + "ID", _options.UbigeoEmisor),
                                new XElement(NsCbc + "AddressTypeCode", "0000"))))),
                new XElement(NsCac + "AccountingCustomerParty",
                    new XElement(NsCac + "Party",
                        new XElement(NsCac + "PartyIdentification",
                            new XElement(NsCbc + "ID",
                                new XAttribute("schemeID", pedido.ClienteTipoDoc),
                                new XAttribute("schemeName", "Documento de Identidad"),
                                new XAttribute("schemeAgencyName", "PE:SUNAT"),
                                new XAttribute("schemeURI", "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo06"),
                                pedido.ClienteNumDoc)),
                        new XElement(NsCac + "PartyLegalEntity",
                            new XElement(NsCbc + "RegistrationName", pedido.ClienteNombre))))
            );
            if (comprobante.Tipo == TipoComprobante.Factura)
            {
                invoice.Add(new XElement(NsCac + "PaymentTerms",
                    new XElement(NsCbc + "ID", "FormaPago"),
                    new XElement(NsCbc + "PaymentMeansID", "Contado")));
            }
            invoice.Add(
                new XElement(NsCac + "TaxTotal",
                    new XElement(NsCbc + "TaxAmount",
                        new XAttribute("currencyID", "PEN"),
                        FormatearMonto(pedido.Igv)),
                    new XElement(NsCac + "TaxSubtotal",
                        new XElement(NsCbc + "TaxableAmount",
                            new XAttribute("currencyID", "PEN"),
                            FormatearMonto(pedido.Subtotal)),
                        new XElement(NsCbc + "TaxAmount",
                            new XAttribute("currencyID", "PEN"),
                            FormatearMonto(pedido.Igv)),
                        new XElement(NsCac + "TaxCategory",
                            new XElement(NsCac + "TaxScheme",
                                new XElement(NsCbc + "ID",
                                    new XAttribute("schemeID", "UN/ECE 5153"),
                                    new XAttribute("schemeAgencyID", "6"),
                                    "1000"),
                                new XElement(NsCbc + "Name", "IGV"),
                                new XElement(NsCbc + "TaxTypeCode", "VAT"))))),
                new XElement(NsCac + "LegalMonetaryTotal",
                    new XElement(NsCbc + "LineExtensionAmount",
                        new XAttribute("currencyID", "PEN"),
                        FormatearMonto(pedido.Subtotal)),
                    new XElement(NsCbc + "TaxInclusiveAmount",
                        new XAttribute("currencyID", "PEN"),
                        FormatearMonto(pedido.Total)),
                    new XElement(NsCbc + "PayableAmount",
                        new XAttribute("currencyID", "PEN"),
                        FormatearMonto(pedido.Total)))
            );
            var numeroLinea = 1;
            foreach (var detalle in pedido.Detalles)
            {
                var igvLinea = Math.Round(detalle.Subtotal * 0.18m, 2, MidpointRounding.AwayFromZero);
                var precioUnitarioConIgv = Math.Round(detalle.PrecioUnitario * 1.18m, 2, MidpointRounding.AwayFromZero);
                invoice.Add(new XElement(NsCac + "InvoiceLine",
                    new XElement(NsCbc + "ID", numeroLinea++),
                    new XElement(NsCbc + "InvoicedQuantity",
                        new XAttribute("unitCode", "NIU"),
                        new XAttribute("unitCodeListID", "UN/ECE rec 20"),
                        new XAttribute("unitCodeListAgencyName", "United Nations Economic Commission for Europe"),
                        detalle.Cantidad.ToString(CultureInfo.InvariantCulture)),
                    new XElement(NsCbc + "LineExtensionAmount",
                        new XAttribute("currencyID", "PEN"),
                        FormatearMonto(detalle.Subtotal)),
                    new XElement(NsCac + "PricingReference",
                        new XElement(NsCac + "AlternativeConditionPrice",
                            new XElement(NsCbc + "PriceAmount",
                                new XAttribute("currencyID", "PEN"),
                                FormatearMonto(precioUnitarioConIgv)),
                            new XElement(NsCbc + "PriceTypeCode",
                                new XAttribute("listName", "Tipo de Precio"),
                                new XAttribute("listAgencyName", "PE:SUNAT"),
                                new XAttribute("listURI", "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo16"),
                                "01"))),
                    new XElement(NsCac + "TaxTotal",
                        new XElement(NsCbc + "TaxAmount",
                            new XAttribute("currencyID", "PEN"),
                            FormatearMonto(igvLinea)),
                        new XElement(NsCac + "TaxSubtotal",
                            new XElement(NsCbc + "TaxableAmount",
                                new XAttribute("currencyID", "PEN"),
                                FormatearMonto(detalle.Subtotal)),
                            new XElement(NsCbc + "TaxAmount",
                                new XAttribute("currencyID", "PEN"),
                                FormatearMonto(igvLinea)),
                            new XElement(NsCac + "TaxCategory",
                                new XElement(NsCbc + "Percent", "18.00"),
                                new XElement(NsCbc + "TaxExemptionReasonCode",
                                    new XAttribute("listAgencyName", "PE:SUNAT"),
                                    new XAttribute("listName", "Afectacion del IGV"),
                                    new XAttribute("listURI", "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo07"),
                                    "10"),
                                new XElement(NsCac + "TaxScheme",
                                    new XElement(NsCbc + "ID",
                                        new XAttribute("schemeID", "UN/ECE 5153"),
                                        new XAttribute("schemeAgencyID", "6"),
                                        "1000"),
                                    new XElement(NsCbc + "Name", "IGV"),
                                    new XElement(NsCbc + "TaxTypeCode", "VAT"))))),
                    new XElement(NsCac + "Item",
                        new XElement(NsCbc + "Description", detalle.Descripcion)),
                    new XElement(NsCac + "Price",
                        new XElement(NsCbc + "PriceAmount",
                            new XAttribute("currencyID", "PEN"),
                            FormatearMonto(detalle.PrecioUnitario)))));
            }
            return new XDocument(new XDeclaration("1.0", "utf-8", null), invoice);
        }

        private string ObtenerCodigoTipo(TipoComprobante tipo)
        {
            return tipo switch
            {
                TipoComprobante.Factura => "01",
                TipoComprobante.Boleta => "03",
                _ => throw new ArgumentOutOfRangeException(nameof(tipo), $"Tipo de comprobante no soportado: {tipo}")
            };
        }

        private string FormatearMonto(decimal valor)
        {
            return valor.ToString("0.00", CultureInfo.InvariantCulture);
        }
    }
}
