using Application.Features.Strategies.Dtos;
using NRules.Fluent.Dsl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Strategies.Rules;

public class CancelRule : Rule
{
    public override void Define()
    {
        StockWorkflow ctx = null;

        When()
            .Match<StockWorkflow>(() => ctx, c => c.Cancelled);

        Then()
            .Do(_ => Execute(ctx));
    }

    private void Execute(StockWorkflow ctx)
    {
        Console.WriteLine($"[{ctx.Symbol}] Kullanıcı iptal etti → Workflow sonlandı - Fiyat: {ctx.CurrentPrice:F2}");
        ctx.Step = -1;
    }
}
