using System.Security.Claims;
using EShop.Ordering.Application.Commands.CancelOrder;
using EShop.Ordering.Application.Commands.CreateOrder;
using EShop.Ordering.Application.DTOs;
using EShop.Ordering.Application.Queries.GetOrderById;
using EShop.Ordering.Application.Queries.GetUserOrders;
using EShop.Ordering.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EShop.Ordering.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var command = new CreateOrderCommand(GetUserId(), request.ShippingAddress, request.Items);
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetOrderById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrderById(Guid id)
    {
        var query = new GetOrderByIdQuery(id, GetUserId());
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/cancel")]
    public async Task<IActionResult> CancelOrder(Guid id)
    {
        var command = new CancelOrderCommand(id, GetUserId());
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetUserOrders(
        [FromQuery] OrderStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetUserOrdersQuery(GetUserId(), status, page, pageSize);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    public record CreateOrderRequest(AddressDto ShippingAddress, List<OrderItemRequest> Items);
}
