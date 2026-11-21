using IntegracionSunatBeta.Api.Application.DTOs;
using IntegracionSunatBeta.Api.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IntegracionSunatBeta.Api.Controllers
{
    [ApiController]
    [Route("api/pedidos")]
    public class PedidosController : ControllerBase
    {
        private readonly IPedidoService _pedidoService;

        public PedidosController(IPedidoService pedidoService)
        {
            _pedidoService = pedidoService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(PedidoDetalleResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PedidoDetalleResponse>> Crear(
            [FromBody] CrearPedidoRequest request,
            CancellationToken ct)
        {
            var response = await _pedidoService.CrearAsync(request, ct);
            return CreatedAtAction(nameof(ObtenerPorId), new { id = response.Id }, response);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(PedidoDetalleResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PedidoDetalleResponse>> ObtenerPorId(Guid id, CancellationToken ct)
        {
            var response = await _pedidoService.ObtenerPorIdAsync(id, ct);
            return Ok(response);
        }
    }
}