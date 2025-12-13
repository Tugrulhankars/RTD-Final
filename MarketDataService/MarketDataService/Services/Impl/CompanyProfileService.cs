using MarketDataService.Dtos;
using MarketDataService.Helpers;

namespace MarketDataService.Services.Impl
{
    public class CompanyProfileService : ICompanyProfileService
    {
        private readonly FinnhubClient _client;
        public CompanyProfileService(FinnhubClient client) => _client = client;
        public async Task<CompanyProfileDto> GetProfileAsync(string ticker)
        {
            return await _client.GetCompanyProfileAsync(ticker);
        }
    }
}
