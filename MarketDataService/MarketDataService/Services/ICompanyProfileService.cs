using MarketDataService.Dtos;

namespace MarketDataService.Services;

public interface ICompanyProfileService
{
    Task<CompanyProfileDto> GetProfileAsync(string ticker);

}
