using MediatR;
using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Application.Features.Strategies.Commands.Create;
public class CreateStrategyCommand:IRequest<CreateStrategyResponse>
{
    [Required(ErrorMessage = "Strateji adı gereklidir")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Strateji adı 3-100 karakter arasında olmalıdır")]
    public string StrategyName { get; set; }
    [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir")]
    public string Description { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir kullanıcı ID'si giriniz")]
    public int UserId { get; set; }
    [Required(ErrorMessage = "Hisse senedi sembolü gereklidir")]
    [StringLength(10, MinimumLength = 2, ErrorMessage = "Hisse senedi sembolü 2-10 karakter arasında olmalıdır")]
    public string StockSymbol { get; set; }
    [Range(1, 1440, ErrorMessage = "Zaman takibi 1-1440 dakika arasında olmalıdır")]
    public int TimeTracking { get; set; }
    [Range(0.1, 100, ErrorMessage = "Toplam zarar yüzdesi 0.1-100 arasında olmalıdır")]
    public decimal? TotalPercentLoss { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Lot sayısı en az 1 olmalıdır")]
    public int Lot { get; set; }
    [Range(0, double.MaxValue, ErrorMessage = "İşlem tutarı 0'dan büyük olmalıdır")]
    public decimal? TransactionAmount { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir hesap ID'si giriniz")]
    public int? AccountId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir portföy ID'si giriniz")]
    public int? PortfolioId { get; set; }
    [Range(0.0167, double.MaxValue, ErrorMessage = "İzleme süresi minimum 1 dakika (0.0167 saat) olmalıdır")]
    public double? DurationHours { get; set; }
    [Range(0.1, 20, ErrorMessage = "Stop Loss yüzdesi 0.1-20 arasında olmalıdır")]
    public decimal? StopLossPercentage { get; set; }
    [Range(0.1, 50, ErrorMessage = "Take Profit yüzdesi 0.1-50 arasında olmalıdır")]
    public decimal? TakeProfitPercentage { get; set; }
    [Range(-50, 0, ErrorMessage = "Entry Threshold yüzdesi -50 ile 0 arasında olmalıdır")]
    public decimal? EntryThresholdPercentage { get; set; }
    [Range(0.1, 100, ErrorMessage = "Max Loss Limit yüzdesi 0.1-100 arasında olmalıdır")]
    public decimal? MaxLossLimitPercentage { get; set; }
}
