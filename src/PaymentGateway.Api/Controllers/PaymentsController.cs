using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Application.DTOs;
using PaymentGateway.Application.Interfaces;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : Controller
{
    private readonly IPaymentService _service;

    public PaymentsController(IPaymentService service)
    {
        _service = service;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaymentResponseDto?>> Get(Guid id)
    {
        var result = await _service.GetAsync(id);
        return result == null ? NotFound() : Ok(result);

    }

    [HttpPost]
    public async Task<ActionResult<PaymentResponseDto>> Create(PaymentRequestDto request)
    {
        var result = await _service.ProcessAsync(request);
        return Ok(result);
    }

}
