namespace IntegracionSunatBeta.Api.Domain.Entities
{
    public class Pedido
    {
        public Guid Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string ClienteTipoDoc { get; set; } = string.Empty;
        public string ClienteNumDoc { get; set; } = string.Empty;
        public string ClienteNombre { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public decimal Igv { get; set; }
        public decimal Total { get; set; }
        public DateTime FechaCreacionUtc { get; set; }
        public List<PedidoDetalle> Detalles { get; set; } = [];
        public Comprobante? Comprobante { get; set; }
    }
}
