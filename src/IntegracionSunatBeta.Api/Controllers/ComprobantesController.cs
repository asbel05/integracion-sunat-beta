using IntegracionSunatBeta.Api.Application.DTOs;
using IntegracionSunatBeta.Api.Application.Interfaces;
using IntegracionSunatBeta.Api.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace IntegracionSunatBeta.Api.Controllers
{
    [ApiController]
    [Route("api/comprobantes")]
    public class ComprobantesController : ControllerBase
    {
        private readonly IComprobanteService _comprobanteService;

        public ComprobantesController(IComprobanteService comprobanteService)
        {
            _comprobanteService = comprobanteService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<ComprobanteResumenResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResponse<ComprobanteResumenResponse>>> Listar(
            [FromQuery] EstadoComprobante? estado,
            [FromQuery] TipoComprobante? tipo,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            var response = await _comprobanteService.ListarAsync(estado, tipo, page, pageSize, ct);
            return Ok(response);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ComprobanteDetalleResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ComprobanteDetalleResponse>> ObtenerPorId(Guid id, CancellationToken ct)
        {
            var response = await _comprobanteService.ObtenerPorIdAsync(id, ct);
            return Ok(response);
        }
    }
}
