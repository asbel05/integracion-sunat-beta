namespace IntegracionSunatBeta.Api.Domain.Entities
{
    public class PedidoDetalle
    {
        public Guid Id { get; set; }
        public Guid PedidoId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
        public Pedido? Pedido { get; set; }
    }
}
