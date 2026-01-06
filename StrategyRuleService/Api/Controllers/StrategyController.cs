using Application.Features.Strategies.Commands.Create;
using Application.Features.Strategies.Commands.Stop;
using Application.Features.Strategies.Commands.UpdatePreferences;
using Application.Features.Strategies.Queries.GetStrategiesByUserId;
using Application.Features.Strategies.Queries.GetCompletedStrategies;
using Application.Features.Strategies.Queries.GetIncompleteStrategies;
using Application.Features.Strategies.Queries.GetStrategyDetail;
using Application.Features.Strategies.Queries.GetStrategySteps;
using Domain.Events;
using Infrastructure.Services.RabbitMQ;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
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
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value.Errors.Select(e => new { Field = x.Key, Error = e.ErrorMessage }))
                .ToList();
            var errorMessages = errors.Select(e => $"{e.Field}: {e.Error}").ToList();
            var errorDetails = string.Join(" | ", errorMessages);
            _logger.LogWarning("Model validation failed. Request: {@Request}, Errors: {Errors}", 
                new { 
                    createStrategyCommand.StrategyName, 
                    createStrategyCommand.StockSymbol, 
                    createStrategyCommand.UserId,
                    createStrategyCommand.DurationHours,
                    createStrategyCommand.StopLossPercentage,
                    createStrategyCommand.TakeProfitPercentage
                }, 
                errorDetails);
            return BadRequest(new CreateStrategyResponse
            {
                Success = false,
                Message = $"Validation hatası: {errorDetails}"
            });
        }
        try
        {
            CreateStrategyResponse response = await _mediator.Send(createStrategyCommand);
            _logger.LogInformation("Strateji başarıyla oluşturuldu: StrategyId={StrategyId}, UserId={UserId}", 
                response.StrategyId, createStrategyCommand.UserId);
            return Accepted(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Strateji oluşturulurken hata oluştu: UserId={UserId}", 
                createStrategyCommand.UserId);
            return StatusCode(500, new CreateStrategyResponse
            {
                Success = false,
                Message = $"Strateji oluşturulurken hata oluştu: {ex.Message}"
            });
        }
    }
    [HttpGet("getStrategiesByUserId/{userId}")]
    public async Task<IActionResult> GetStrategiesByUserId(int userId)
    {
        try
        {
            var query = new GetStrategiesByUserIdQuery(userId);
            var response = await _mediator.Send(query);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kullanıcı stratejileri alınırken hata oluştu: UserId={UserId}", userId);
            return StatusCode(500, new { Success = false, Message = $"Stratejiler alınırken hata oluştu: {ex.Message}" });
        }
    }
    [HttpGet("getCompletedStrategies/{userId}")]
    public async Task<IActionResult> GetCompletedStrategies(int userId)
    {
        try
        {
            var query = new GetCompletedStrategiesQuery(userId);
            var response = await _mediator.Send(query);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tamamlanmış stratejiler alınırken hata oluştu: UserId={UserId}", userId);
            return StatusCode(500, new { Success = false, Message = $"Tamamlanmış stratejiler alınırken hata oluştu: {ex.Message}" });
        }
    }
    [HttpGet("getIncompleteStrategies/{userId}")]
    public async Task<IActionResult> GetIncompleteStrategies(int userId)
    {
        try
        {
            var query = new GetIncompleteStrategiesQuery(userId);
            var response = await _mediator.Send(query);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tamamlanmamış stratejiler alınırken hata oluştu: UserId={UserId}", userId);
            return StatusCode(500, new { Success = false, Message = $"Tamamlanmamış stratejiler alınırken hata oluştu: {ex.Message}" });
        }
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
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Strateji bulunamadı: StrategyId={StrategyId}, UserId={UserId}", 
                strategyId, userId);
            return NotFound(new { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Strateji detayı alınırken hata oluştu: StrategyId={StrategyId}, UserId={UserId}", 
                strategyId, userId);
            return StatusCode(500, new { Success = false, Message = ex.Message });
        }
    }
    [HttpGet("steps/{strategyId}/{userId}")]
    public async Task<IActionResult> GetStrategySteps(int strategyId, int userId)
    {
        try
        {
            var query = new GetStrategyStepsQuery(strategyId, userId);
            var response = await _mediator.Send(query);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Strateji bulunamadı: StrategyId={StrategyId}, UserId={UserId}", 
                strategyId, userId);
            return NotFound(new { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Strateji adımları alınırken hata oluştu: StrategyId={StrategyId}, UserId={UserId}", 
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
    [HttpPut("updatePreferences")]
    public async Task<IActionResult> UpdateStrategyPreferences([FromBody] UpdateStrategyPreferencesCommand command)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value.Errors.Select(e => e.ErrorMessage))
                .ToList();
            _logger.LogWarning("Model validation failed: {Errors}", string.Join(", ", errors));
            return BadRequest(new UpdateStrategyPreferencesResponse
            {
                Success = false,
                Message = string.Join(", ", errors),
                StrategyId = command.StrategyId
            });
        }
        try
        {
            var response = await _mediator.Send(command);
            if (response.Success)
            {
                _logger.LogInformation("Strateji tercihleri başarıyla güncellendi: StrategyId={StrategyId}, UserId={UserId}", 
                    command.StrategyId, command.UserId);
                return Ok(response);
            }
            return BadRequest(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Strateji tercihleri güncellenirken hata oluştu: StrategyId={StrategyId}", command.StrategyId);
            return StatusCode(500, new UpdateStrategyPreferencesResponse
            {
                Success = false,
                Message = $"Strateji tercihleri güncellenirken hata oluştu: {ex.Message}",
                StrategyId = command.StrategyId
            });
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
    [HttpGet("verifyExecution/{strategyId}/{userId}")]
    public async Task<IActionResult> VerifyStrategyExecution(int strategyId, int userId)
    {
        try
        {
            var query = new GetStrategyDetailQuery(strategyId, userId);
            var strategyDetail = await _mediator.Send(query);
            if (strategyDetail?.Strategy == null)
            {
                return NotFound(new { Success = false, Message = "Strateji bulunamadı" });
            }
            var strategy = strategyDetail.Strategy;
            var events = strategyDetail.Events ?? new List<StrategyEventDto>();
            var strategyStatus = Enum.TryParse<Domain.Enums.StrategyStatus>(strategy.Status, out var parsedStatus) 
                ? parsedStatus 
                : Domain.Enums.StrategyStatus.Inactive;
            int? currentStepInt = null;
            if (!string.IsNullOrEmpty(strategy.CurrentStep))
            {
                var stepMatch = System.Text.RegularExpressions.Regex.Match(strategy.CurrentStep, @"Step\s+(\d+)");
                if (stepMatch.Success && int.TryParse(stepMatch.Groups[1].Value, out var step))
                {
                    currentStepInt = step;
                }
            }
            var verification = new
            {
                StrategyExists = true,
                StrategyId = strategy.Id,
                StrategyName = strategy.StrategyName,
                Status = strategy.Status,
                CurrentStep = currentStepInt ?? -1,
                IsActive = strategy.IsActive,
                StartDate = strategy.StartDate,
                LastProcessed = events.OrderByDescending(e => e.Timestamp).FirstOrDefault()?.Timestamp,
                TotalEvents = events.Count,
                EventsByStep = events.GroupBy(e => e.Step).Select(g => new
                {
                    Step = g.Key,
                    Count = g.Count(),
                    LastEvent = g.OrderByDescending(e => e.Timestamp).FirstOrDefault()?.Timestamp
                }).ToList(),
                RecentEvents = events.OrderByDescending(e => e.Timestamp).Take(5).Select(e => new
                {
                    e.Id,
                    e.Step,
                    e.RuleName,
                    e.Action,
                    e.Timestamp,
                    e.Reason,
                    e.Price
                }).ToList(),
                WorkerServiceRunning = strategy.IsActive && strategyStatus == Domain.Enums.StrategyStatus.Active,
                StepProgression = new
                {
                    HasStep0 = events.Any(e => e.Step == 0),
                    HasStep1 = events.Any(e => e.Step == 1),
                    HasStep2 = events.Any(e => e.Step == 2),
                    HasStep3 = events.Any(e => e.Step == 3),
                    HasCompleted = events.Any(e => e.Step == -1)
                },
                HasTradeActions = events.Any(e => e.Action == "BUY" || e.Action == "SELL"),
                LastActionTime = events
                    .Where(e => e.Action == "BUY" || e.Action == "SELL")
                    .OrderByDescending(e => e.Timestamp)
                    .FirstOrDefault()?.Timestamp,
                TimeSinceLastEvent = events.Any() 
                    ? (DateTime.Now - events.OrderByDescending(e => e.Timestamp).First().Timestamp).TotalSeconds
                    : (double?)null,
                IsWorking = strategy.IsActive && 
                           strategyStatus == Domain.Enums.StrategyStatus.Active && 
                           events.Any() &&
                           (DateTime.Now - events.OrderByDescending(e => e.Timestamp).First().Timestamp).TotalSeconds < 300,
                Recommendations = new List<string>()
            };
            var recommendations = new List<string>();
            if (!strategy.IsActive)
            {
                recommendations.Add("⚠️ Strateji aktif değil (IsActive = false)");
            }
            if (strategyStatus != Domain.Enums.StrategyStatus.Active)
            {
                recommendations.Add($"⚠️ Strateji durumu: {strategy.Status} (Beklenen: Active)");
            }
            if (!events.Any())
            {
                recommendations.Add("❌ Hiç event kaydı yok - Worker service stratejiyi işlemiyor olabilir");
            }
            else
            {
                var lastEventTime = events.OrderByDescending(e => e.Timestamp).First().Timestamp;
                var secondsSinceLastEvent = (DateTime.Now - lastEventTime).TotalSeconds;
                if (secondsSinceLastEvent > 300)
                {
                    recommendations.Add($"⚠️ Son event {Math.Round(secondsSinceLastEvent / 60, 1)} dakika önce - Worker service çalışmıyor olabilir");
                }
                if (!verification.StepProgression.HasStep0)
                {
                    recommendations.Add("⚠️ Step 0 (TimeCheckRule) hiç tetiklenmemiş");
                }
            }
            if (currentStepInt == null || currentStepInt == 0)
            {
                recommendations.Add("ℹ️ Strateji hala Step 0'da - Piyasa saatini bekliyor olabilir");
            }
            var result = new
            {
                Success = true,
                Verification = verification,
                Recommendations = recommendations,
                Conclusion = verification.IsWorking 
                    ? "✅ Strateji çalışıyor görünüyor" 
                    : "❌ Strateji çalışmıyor veya sorun var"
            };
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Strateji doğrulama hatası: StrategyId={StrategyId}, UserId={UserId}", strategyId, userId);
            return StatusCode(500, new { Success = false, Message = $"Doğrulama hatası: {ex.Message}" });
        }
    }
    [HttpGet("events/{strategyId}/{userId}")]
    public async Task<IActionResult> GetStrategyEvents(int strategyId, int userId, [FromQuery] int limit = 50)
    {
        try
        {
            var query = new GetStrategyDetailQuery(strategyId, userId);
            var strategyDetail = await _mediator.Send(query);
            if (strategyDetail?.Strategy == null)
            {
                return NotFound(new { Success = false, Message = "Strateji bulunamadı" });
            }
            if (strategyDetail.Strategy.UserId != userId)
            {
                return Forbid();
            }
            var events = (strategyDetail.Events ?? new List<StrategyEventDto>())
                .OrderByDescending(e => e.Timestamp)
                .Take(limit)
                .Select(e => new
                {
                    e.Id,
                    e.Step,
                    e.RuleName,
                    e.Action,
                    e.Timestamp,
                    e.Reason,
                    e.Price,
                    TimeAgo = (DateTime.Now - e.Timestamp).TotalSeconds
                })
                .ToList();
            return Ok(new
            {
                Success = true,
                StrategyId = strategyId,
                TotalEvents = strategyDetail.Events?.Count ?? 0,
                Events = events,
                Summary = new
                {
                    EventsByStep = events.GroupBy(e => e.Step).Select(g => new
                    {
                        Step = g.Key,
                        Count = g.Count()
                    }).ToList(),
                    EventsByAction = events.GroupBy(e => e.Action).Select(g => new
                    {
                        Action = g.Key,
                        Count = g.Count()
                    }).ToList()
                }
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Event logları alınırken hata: StrategyId={StrategyId}, UserId={UserId}", strategyId, userId);
            return StatusCode(500, new { Success = false, Message = ex.Message });
        }
    }
    [HttpGet("workerStatus")]
    public IActionResult GetWorkerServiceStatus()
    {
        try
        {
            var status = new
            {
                WorkerServiceExpected = true,
                Message = "Worker service StrategyProcessingHostedService olarak çalışıyor olmalı",
                CheckInstructions = new[]
                {
                    "1. StrategyRuleService.Worker veya API projesinin çalıştığından emin olun",
                    "2. Logları kontrol edin: 'StrategyProcessingHostedService started' mesajını arayın",
                    "3. StrategyRuleService.Worker/Worker.cs veya Application/Services/StrategyProcessingHostedService.cs dosyasını kontrol edin",
                    "4. appsettings.json'da 'StrategyProcessing:IntervalSeconds' ayarını kontrol edin"
                }
            };
            return Ok(status);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Success = false, Message = ex.Message });
        }
    }
}
