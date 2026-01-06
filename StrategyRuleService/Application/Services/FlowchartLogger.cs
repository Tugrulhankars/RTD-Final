using Application.Features.Strategies.Dtos;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
namespace Application.Services;
public class FlowchartLogger
{
    private readonly ILogger<FlowchartLogger> _logger;
    public FlowchartLogger(ILogger<FlowchartLogger> logger)
    {
        _logger = logger;
    }
    public void LogFlowchart(StockWorkflow ctx, string currentRule, string decision = null, bool? decisionResult = null, string reason = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        sb.AppendLine($"║  STRATEJİ AKIŞ DİYAGRAMI - {ctx.Symbol} (ID: {ctx.StrategyId})                    ║");
        sb.AppendLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        sb.AppendLine();
        sb.AppendLine("                    ┌─────────────────────────────┐");
        sb.AppendLine("                    │   🔄 WORKER SERVICE         │");
        sb.AppendLine("                    │   Sürekli Döngü            │");
        sb.AppendLine("                    │   Her 5 saniyede bir       │");
        sb.AppendLine("                    └───────────┬───────────────┘");
        sb.AppendLine("                                │");
        sb.AppendLine("                                ▼");
        var step0Active = currentRule == "TimeCheckRule" && ctx.Step == 0;
        var step0Completed = ctx.Step > 0;
        var step0Status = GetStepStatus(step0Active, step0Completed);
        sb.AppendLine($"                    ┌─────────────────────────────┐");
        sb.AppendLine($"                    │ {step0Status} STEP 0: PİYASA KONTROLÜ    │");
        sb.AppendLine($"                    │ TimeCheckRule               │");
        sb.AppendLine($"                    │ Piyasa: {(ctx.MarketOpen ? "✅ AÇIK" : "❌ KAPALI")}              │");
        if (step0Active && decisionResult.HasValue)
        {
            sb.AppendLine($"                    │ Karar: {(decisionResult.Value ? "✅ EVET" : "❌ HAYIR")}                    │");
            if (!string.IsNullOrEmpty(reason))
            {
                sb.AppendLine($"                    │ {Truncate(reason, 27)} │");
            }
        }
        sb.AppendLine($"                    └───────────┬───────────────┘");
        if (step0Active && decisionResult.HasValue && !decisionResult.Value)
        {
            sb.AppendLine("                                │ ❌ HAYIR");
            sb.AppendLine("                                ▼");
            sb.AppendLine("                    ┌─────────────────────────────┐");
            sb.AppendLine("                    │   ⛔ STRATEJİ SONLANDI      │");
            sb.AppendLine("                    │   Step: -1                  │");
            sb.AppendLine($"                    │   Sebep: {Truncate(reason ?? "Piyasa kapalı", 27)} │");
            sb.AppendLine("                    └─────────────────────────────┘");
        }
        else if (step0Completed)
        {
            sb.AppendLine("                                │ ✅ EVET");
            sb.AppendLine("                                ▼");
        }
        if (ctx.Step >= 1)
        {
            var step1Active = currentRule == "PortfolioCheckRule" && ctx.Step == 1;
            var step1Completed = ctx.Step > 1;
            var step1Status = GetStepStatus(step1Active, step1Completed);
            var hasPosition = ctx.InPortfolio;
            sb.AppendLine($"                    ┌─────────────────────────────┐");
            sb.AppendLine($"                    │ {step1Status} STEP 1: PORTFÖY KONTROLÜ │");
            sb.AppendLine($"                    │ PortfolioCheckRule          │");
            sb.AppendLine($"                    │ Pozisyon: {(hasPosition ? "✅ VAR" : "❌ YOK")}                │");
            if (step1Active && decisionResult.HasValue)
            {
                sb.AppendLine($"                    │ Karar: {(decisionResult.Value ? "✅ EVET" : "❌ HAYIR")}                    │");
                if (!string.IsNullOrEmpty(reason))
                {
                    sb.AppendLine($"                    │ {Truncate(reason, 27)} │");
                }
            }
            sb.AppendLine($"                    └───────────┬───────────────┘");
            if (step1Completed)
            {
                sb.AppendLine("                                │");
                sb.AppendLine("                    ┌──────────┴──────────┐");
                sb.AppendLine("                    │                     │");
                sb.AppendLine("                    ▼                     ▼");
            }
        }
        if (ctx.Step >= 2 && ctx.InPortfolio)
        {
            var step2Active = currentRule == "SellRule" && ctx.Step == 2;
            var step2Completed = ctx.Step == -1;
            var step2Status = GetStepStatus(step2Active, step2Completed);
            sb.AppendLine($"    ┌─────────────────────────────────────────────┐");
            sb.AppendLine($"    │ {step2Status} STEP 2: SATIŞ KONTROLÜ (DÖNGÜSEL)  │");
            sb.AppendLine($"    │ SellRule                                   │");
            sb.AppendLine($"    │ Fiyat: ₺{ctx.CurrentPrice:F2}                      │");
            if (ctx.BuyPrice.HasValue)
            {
                sb.AppendLine($"    │ Alış Fiyatı: ₺{ctx.BuyPrice.Value:F2}              │");
                var profitLoss = ((ctx.CurrentPrice - ctx.BuyPrice.Value) / ctx.BuyPrice.Value) * 100;
                sb.AppendLine($"    │ Kar/Zarar: {profitLoss:+#0.00;-#0.00}%                  │");
            }
            if (step2Active && decisionResult.HasValue)
            {
                sb.AppendLine($"    │ Kontrol: {(decisionResult.Value ? "✅ SATIŞ ŞARTI" : "⏳ BEKLE")}          │");
                if (!string.IsNullOrEmpty(reason))
                {
                    sb.AppendLine($"    │ {Truncate(reason, 43)} │");
                }
            }
            sb.AppendLine($"    └───────────┬───────────────────────────────┘");
            if (step2Active)
            {
                sb.AppendLine("                │");
                sb.AppendLine("                │ 🔄 Döngüye Dön");
                sb.AppendLine("                ▼");
                sb.AppendLine("    ┌─────────────────────────────────────────────┐");
                sb.AppendLine("    │   ⏳ Bir sonraki tick'i bekle (5 saniye)     │");
                sb.AppendLine("    └─────────────────────────────────────────────┘");
            }
        }
        if (ctx.Step >= 3 && !ctx.InPortfolio)
        {
            var step3Active = currentRule == "BuyRule" && ctx.Step == 3;
            var step3Completed = ctx.Step == -1;
            var step3Status = GetStepStatus(step3Active, step3Completed);
            sb.AppendLine($"                                    ┌─────────────────────────────────────────────┐");
            sb.AppendLine($"                                    │ {step3Status} STEP 3: ALIM KONTROLÜ (DÖNGÜSEL)   │");
            sb.AppendLine($"                                    │ BuyRule                                     │");
            sb.AppendLine($"                                    │ Fiyat: ₺{ctx.CurrentPrice:F2}                      │");
            if (ctx.OpeningPrice > 0)
            {
                sb.AppendLine($"                                    │ Açılış: ₺{ctx.OpeningPrice:F2}                  │");
                var entryPrice = ctx.OpeningPrice * (1 + (ctx.EntryThresholdPercent / 100));
                sb.AppendLine($"                                    │ Entry: ₺{entryPrice:F2}                   │");
            }
            if (step3Active && decisionResult.HasValue)
            {
                sb.AppendLine($"                                    │ Kontrol: {(decisionResult.Value ? "✅ ALIM ŞARTI" : "⏳ BEKLE")}          │");
                if (!string.IsNullOrEmpty(reason))
                {
                    sb.AppendLine($"                                    │ {Truncate(reason, 43)} │");
                }
            }
            sb.AppendLine($"                                    └───────────┬───────────────────────────────┘");
            if (step3Active)
            {
                sb.AppendLine("                                                │");
                sb.AppendLine("                                                │ 🔄 Döngüye Dön");
                sb.AppendLine("                                                ▼");
                sb.AppendLine("                                    ┌─────────────────────────────────────────────┐");
                sb.AppendLine("                                    │   ⏳ Bir sonraki tick'i bekle (5 saniye)     │");
                sb.AppendLine("                                    └─────────────────────────────────────────────┘");
            }
        }
        if (ctx.Step == -1)
        {
            sb.AppendLine();
            sb.AppendLine("                    ┌─────────────────────────────┐");
            sb.AppendLine("                    │   ✅ STRATEJİ TAMAMLANDI    │");
            sb.AppendLine("                    │   Step: -1                  │");
            if (!string.IsNullOrEmpty(reason))
            {
                sb.AppendLine($"                    │   {Truncate(reason, 27)} │");
            }
            sb.AppendLine("                    └─────────────────────────────┘");
        }
        sb.AppendLine();
        sb.AppendLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        sb.AppendLine("║  DURUM ÖZETİ                                                               ║");
        sb.AppendLine("╠══════════════════════════════════════════════════════════════════════════════╣");
        sb.AppendLine($"║  Mevcut Adım: {Truncate(GetStepName(ctx.Step), 60),-60} ║");
        sb.AppendLine($"║  Aktif Kural: {Truncate(currentRule, 60),-60} ║");
        sb.AppendLine($"║  Fiyat: ₺{ctx.CurrentPrice:F2}{new string(' ', 56)} ║");
        sb.AppendLine($"║  Toplam Zarar: {ctx.TotalLossPercent:F2}%{new string(' ', 52)} ║");
        sb.AppendLine($"║  Pozisyon: {Truncate(ctx.InPortfolio ? "Açık" : "Kapalı", 60),-60} ║");
        if (!string.IsNullOrEmpty(reason))
        {
            sb.AppendLine($"║  Açıklama: {Truncate(reason, 60),-60} ║");
        }
        sb.AppendLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        sb.AppendLine();
        _logger.LogInformation(sb.ToString());
    }
    public void LogSimpleFlowchart(StockWorkflow ctx, string currentRule, string action, string reason = null)
    {
        var stepName = GetStepName(ctx.Step);
        var status = ctx.Step == -1 ? "✅ TAMAMLANDI" : "🔄 ÇALIŞIYOR";
        var message = $"[{ctx.Symbol}] {status} | Step: {ctx.Step} ({stepName}) | Kural: {currentRule} | Aksiyon: {action}";
        if (!string.IsNullOrEmpty(reason))
        {
            message += $" | Sebep: {reason}";
        }
        message += $" | Fiyat: ₺{ctx.CurrentPrice:F2}";
        if (ctx.Step == 2 || ctx.Step == 3)
        {
            message += " | 🔄 DÖNGÜSEL";
        }
        _logger.LogInformation(message);
    }
    private string GetStepStatus(bool isActive, bool isCompleted)
    {
        if (isActive) return ">> AKTİF <<";
        if (isCompleted) return "✅ TAMAM";
        return "⏳ BEKLE";
    }
    private string GetStepName(int step)
    {
        return step switch
        {
            0 => "Piyasa Kontrolü",
            1 => "Portföy Kontrolü",
            2 => "Satış Kontrolü (Döngüsel)",
            3 => "Alım Kontrolü (Döngüsel)",
            -1 => "Tamamlandı",
            _ => "Bilinmiyor"
        };
    }
    private string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return new string(' ', maxLength);
        if (text.Length <= maxLength) return text.PadRight(maxLength);
        return text.Substring(0, maxLength - 3) + "...";
    }
}
