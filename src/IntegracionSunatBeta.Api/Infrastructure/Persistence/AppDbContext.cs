using IntegracionSunatBeta.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntegracionSunatBeta.Api.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Pedido> Pedidos => Set<Pedido>();
        public DbSet<PedidoDetalle> PedidoDetalles => Set<PedidoDetalle>();
        public DbSet<Comprobante> Comprobantes => Set<Comprobante>();
        public DbSet<ComprobanteIntento> ComprobanteIntentos => Set<ComprobanteIntento>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasSequence<int>("SeqPedidoCodigo").StartsAt(1).IncrementsBy(1);
            modelBuilder.HasSequence<int>("SeqComprobanteB001").StartsAt(1).IncrementsBy(1);
            modelBuilder.HasSequence<int>("SeqComprobanteF001").StartsAt(1).IncrementsBy(1);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
