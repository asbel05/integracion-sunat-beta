using IntegracionSunatBeta.Api.Application.Interfaces;
using IntegracionSunatBeta.Api.Domain.Entities;
using IntegracionSunatBeta.Api.Domain.Enums;
using IntegracionSunatBeta.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IntegracionSunatBeta.Api.Infrastructure.Repositories
{
    public class ComprobanteRepository : IComprobanteRepository
    {
        private readonly AppDbContext _context;
        public ComprobanteRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AgregarIntentoAsync(ComprobanteIntento intento, CancellationToken ct = default)
        {
            await _context.ComprobanteIntentos.AddAsync(intento, ct);
        }

        public async Task GuardarCambiosAsync(CancellationToken ct = default)
        {
            await _context.SaveChangesAsync(ct);
        }

        public async Task<(IReadOnlyList<Comprobante> Items, int TotalCount)> ListarAsync(
            EstadoComprobante? estado = null, TipoComprobante? tipo = null,
            int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            var query = _context.Comprobantes
                .AsNoTracking()
                .Include(c => c.Pedido)
                .AsQueryable();
            if (estado.HasValue)
                query = query.Where(c => c.Estado == estado.Value);
            if (tipo.HasValue)
                query = query.Where(c => c.Tipo == tipo.Value);
            var totalCount = await query.CountAsync(ct);
            var items = await query
                .OrderByDescending(c => c.ActualizadoUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);
            return (items, totalCount);
        }

        public async Task<Comprobante?> ObtenerPorIdConDetallesIntentosAsync(Guid id, CancellationToken ct = default)
        {
            return await _context.Comprobantes
                .Include(c => c.Pedido)
                    .ThenInclude(p => p!.Detalles)
                .Include(c => c.HistorialIntentos.OrderByDescending(i => i.NumeroIntento))
                .AsSplitQuery()
                .FirstOrDefaultAsync(c => c.Id == id, ct);
        }

        public async Task<List<Comprobante>> ObtenerPorIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
        {
            return await _context.Comprobantes
                .Where(c => ids.Contains(c.Id))
                .ToListAsync(ct);
        }
    }
}
