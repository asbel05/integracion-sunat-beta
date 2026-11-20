using IntegracionSunatBeta.Api.Application.Interfaces;
using IntegracionSunatBeta.Api.Domain.Entities;
using IntegracionSunatBeta.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IntegracionSunatBeta.Api.Infrastructure.Repositories
{
    public class PedidoRepository : IPedidoRepository
    {
        private readonly AppDbContext _context;

        public PedidoRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AgregarAsync(Pedido pedido, CancellationToken ct = default)
        {
            await _context.Pedidos.AddAsync(pedido, ct);
        }

        public async Task GuardarCambiosAsync(CancellationToken ct = default)
        {
            await _context.SaveChangesAsync(ct);
        }

        public async Task<Pedido?> ObtenerPorIdConDetalleAsync(Guid id, CancellationToken ct = default)
        {
            return await _context.Pedidos
                .AsNoTracking()
                .Include(p => p.Detalles)
                .Include(p => p.Comprobante)
                .FirstOrDefaultAsync(p => p.Id == id, ct);
        }

        public async Task<int> ObtenerSiguienteCodigoPedidoAsync(CancellationToken ct = default)
        {
            return await _context.Database
                .SqlQueryRaw<int>("SELECT NEXT VALUE FOR dbo.SeqPedidoCodigo AS [Value]")
                .SingleAsync(ct);
        }

        public async Task<int> ObtenerSiguienteCorrelativoAsync(string serie, CancellationToken ct = default)
        {
            var sql = serie switch
            {
                "B001" => "SELECT NEXT VALUE FOR dbo.SeqComprobanteB001 AS [Value]",
                "F001" => "SELECT NEXT VALUE FOR dbo.SeqComprobanteF001 AS [Value]",
                _ => throw new ArgumentException($"Serie no soportada: {serie}", nameof(serie))
            };
            return await _context.Database
                .SqlQueryRaw<int>(sql)
                .SingleAsync(ct);
        }
    }
}
