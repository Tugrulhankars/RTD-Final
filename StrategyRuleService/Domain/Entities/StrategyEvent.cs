using Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Domain.Entities;
public class StrategyEvent : BaseEntity<int>
{
    public int StrategyId { get; set; }
    public int Step { get; set; }
    public string RuleName { get; set; }
    public string Action { get; set; }
    public string Reason { get; set; }
    public decimal Price { get; set; }
    public DateTime Timestamp { get; set; }
}
