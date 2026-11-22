namespace IntegracionSunatBeta.Api.Infrastructure.Sunat
{
    public class SunatOptions
    {
        public const string SectionName = "Sunat";
        public string EndpointUrl { get; set; } = "https://e-beta.sunat.gob.pe/ol-ti-itcpfegem-beta/billService";
        public string RucEmisor { get; set; } = "20000000001";
        public string RazonSocialEmisor { get; set; } = "EMPRESA DEMO SUNAT BETA SAC";
        public string UbigeoEmisor { get; set; } = "150101";
        public string UsuarioSol { get; set; } = string.Empty;
        public string ClaveSol { get; set; } = string.Empty;
        public string CertificadoBase64 { get; set; } = string.Empty;
        public string CertificadoPassword { get; set; } = string.Empty;
        public bool SimularErrorTransitorio { get; set; } = false;
    }
}
