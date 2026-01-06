using MediatR;
using System.ComponentModel.DataAnnotations;
namespace Application.Features.Strategies.Commands.UpdatePreferences;
public class UpdateStrategyPreferencesCommand : IRequest<UpdateStrategyPreferencesResponse>
{
    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir strateji ID'si giriniz")]
    public int StrategyId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir kullanıcı ID'si giriniz")]
    public int UserId { get; set; }
    [Required(ErrorMessage = "Hisse senedi sembolü gereklidir")]
    public string Ticker { get; set; }
    [Range(0.1, 20, ErrorMessage = "Stop Loss yüzdesi 0.1-20 arasında olmalıdır")]
    public decimal StopLossPercentage { get; set; }
    [Range(0.1, 50, ErrorMessage = "Take Profit yüzdesi 0.1-50 arasında olmalıdır")]
    public decimal TakeProfitPercentage { get; set; }
    [Range(-50, 0, ErrorMessage = "Entry Threshold yüzdesi -50 ile 0 arasında olmalıdır")]
    public decimal EntryThresholdPercentage { get; set; }
    [Range(0.1, 100, ErrorMessage = "Max Loss Limit yüzdesi 0.1-100 arasında olmalıdır")]
    public decimal MaxLossLimitPercentage { get; set; }
}
