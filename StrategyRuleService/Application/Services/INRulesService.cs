using System;
using System.Threading.Tasks;

namespace Application.Services
{
    public interface INRulesService
    {
        Task ProcessRulesAsync();
        Task AddStrategyAsync(string strategyName, object context);
        Task RemoveStrategyAsync(string strategyName);
    }
}
