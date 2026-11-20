using IntegracionSunatBeta.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntegracionSunatBeta.Api.Infrastructure.Persistence.Configurations
{
    public class PedidoDetalleConfiguration : IEntityTypeConfiguration<PedidoDetalle>
    {
        public void Configure(EntityTypeBuilder<PedidoDetalle> builder)
        {
            builder.ToTable("PedidoDetalles");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();

            builder.Property(x => x.Descripcion).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Cantidad).IsRequired();
            builder.Property(x => x.PrecioUnitario).HasPrecision(18, 2);
            builder.Property(x => x.Subtotal).HasPrecision(18, 2);
        }
    }
}
