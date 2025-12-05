using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserFeed.Domain.Interfaces;

namespace UserFeed.Api.Controllers;

[ApiController]
[Route("api/v1")]
[Tags("Orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet("orders")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUserOrders()
    {
        var token = Request.Headers["Authorization"].ToString();
        
        var orders = await _orderService.GetUserOrdersAsync(token);
        
        return Ok(orders);
    }
}
