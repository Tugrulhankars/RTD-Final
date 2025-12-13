using Application.Features.Strategies.Commands.Create;
using Application.Features.Strategies.Commands.Stop;
using Application.Features.Strategies.Queries.GetStrategiesByUserId;
using Application.Features.Strategies.Queries.GetCompletedStrategies;
using Application.Features.Strategies.Queries.GetIncompleteStrategies;
using Application.Features.Strategies.Queries.GetStrategyDetail;
using Domain.Events;
using Infrastructure.Services.RabbitMQ;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class StrategyController : ControllerBase
{

    private readonly IMediator _mediator;
    private readonly IRabbitMQPublisher _rabbitMQPublisher;
    private readonly ILogger<StrategyController> _logger;

    public StrategyController(IMediator mediator, IRabbitMQPublisher rabbitMQPublisher, ILogger<StrategyController> logger)
    {
        _mediator = mediator;
        _rabbitMQPublisher = rabbitMQPublisher;
        _logger = logger;
    }

    [HttpPost("createStrategy")]
    public async Task<IActionResult> CreateStraregy([FromBody] CreateStrategyCommand createStrategyCommand)
    {
        CreateStrategyResponse response=await _mediator.Send(createStrategyCommand);
        return Ok(response);
    }

    [HttpGet("getStrategiesByUserId/{userId}")]
    public async Task<IActionResult> GetStrategiesByUserId(int userId)
    {
        var query = new GetStrategiesByUserIdQuery(userId);
        var response = await _mediator.Send(query);
        return Ok(response);
    }

    [HttpGet("getCompletedStrategies/{userId}")]
    public async Task<IActionResult> GetCompletedStrategies(int userId)
    {
        var query = new GetCompletedStrategiesQuery(userId);
        var response = await _mediator.Send(query);
        return Ok(response);
    }

    [HttpGet("getIncompleteStrategies/{userId}")]
    public async Task<IActionResult> GetIncompleteStrategies(int userId)
    {
        var query = new GetIncompleteStrategiesQuery(userId);
        var response = await _mediator.Send(query);
        return Ok(response);
    }

    [HttpGet("getStrategyDetail/{strategyId}/{userId}")]
    public async Task<IActionResult> GetStrategyDetail(int strategyId, int userId)
    {
        try
        {
            var query = new GetStrategyDetailQuery(strategyId, userId);
            var response = await _mediator.Send(query);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Strateji detayı alınırken hata oluştu: StrategyId={StrategyId}, UserId={UserId}", 
                strategyId, userId);
            return StatusCode(500, new { Success = false, Message = ex.Message });
        }
    }

    [HttpPost("stopStrategy")]
    public async Task<IActionResult> StopStrategy([FromBody] StopStrategyCommand command)
    {
        try
        {
            var response = await _mediator.Send(command);
            if (response.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Success = false, Message = ex.Message });
        }
    }

    [HttpPost("testRabbitMQ")]
    public async Task<IActionResult> TestRabbitMQ()
    {
        try
        {
            var testEvent = new StrategyNotificationEvent
            {
                StrategyId = 999,
                UserId = 1,
                StrategyName = "Test Strategy",
                StockSymbol = "TEST",
                Status = "Active",
                Action = "TEST",
                CurrentPrice = 100.50m,
                Timestamp = DateTime.Now,
                Reason = "RabbitMQ bağlantı testi"
            };

            await _rabbitMQPublisher.PublishAsync(testEvent, "strategy-notifications");
            
            _logger.LogInformation("Test event'i RabbitMQ'ya gönderildi");
            
            return Ok(new { Success = true, Message = "Test event'i RabbitMQ'ya başarıyla gönderildi. Queue: strategy-notifications" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RabbitMQ test event'i gönderilirken hata oluştu");
            return StatusCode(500, new { Success = false, Message = $"RabbitMQ test hatası: {ex.Message}" });
        }
    }
}
