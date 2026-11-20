using IntegracionSunatBeta.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntegracionSunatBeta.Api.Infrastructure.Persistence.Configurations
{
    public class PedidoConfiguration : IEntityTypeConfiguration<Pedido>
    {
        public void Configure(EntityTypeBuilder<Pedido> builder)
        {
            builder.ToTable("Pedidos");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();

            builder.Property(x => x.Codigo).HasMaxLength(20).IsRequired();
            builder.HasIndex(x => x.Codigo).IsUnique();

            builder.Property(x => x.ClienteTipoDoc).HasMaxLength(2).IsRequired();
            builder.Property(x => x.ClienteNumDoc).HasMaxLength(11).IsRequired();
            builder.Property(x => x.ClienteNombre).HasMaxLength(150).IsRequired();

            builder.Property(x => x.Subtotal).HasPrecision(18, 2);
            builder.Property(x => x.Igv).HasPrecision(18, 2);
            builder.Property(x => x.Total).HasPrecision(18, 2);
            builder.Property(x => x.FechaCreacionUtc).IsRequired();

            builder.HasMany(x => x.Detalles)
                .WithOne(d => d.Pedido)
                .HasForeignKey(d => d.PedidoId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Comprobante)
                .WithOne(d => d.Pedido)
                .HasForeignKey<Comprobante>(c => c.PedidoId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
