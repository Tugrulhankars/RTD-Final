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
    public string StrategyName { get; set; }
    
    public string Description { get; set; }
    
    public int UserId { get; set; }
    public string StockSymbol { get; set; }
    public int TimeTracking { get; set; }
    public decimal? TotalPercentLoss { get; set; }
    public int Lot { get; set; }//kaç lot için bu strateji çalışacak 
    public decimal? TransactionAmount { get; set; }
    public int? AccountId { get; set; }
    public int? PortfolioId { get; set; }




}
