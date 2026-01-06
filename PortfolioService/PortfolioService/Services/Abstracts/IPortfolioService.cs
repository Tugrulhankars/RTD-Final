using PortfolioService.Dtos.Request;
using PortfolioService.Dtos.Response;
using PortfolioService.Protos;

namespace PortfolioService.Services.Abstracts;

public interface IPortfolioService
{
    Task CreatePortfolio(CreatePortfolioRequest request);

    Task<bool> HasStockInPortfolioAsync(int userId, string symbol);

    Task<List<GetAllPortfolioResponse>> GetAllPortfolioByUserAsync(int userId);

    Task<GetAllPortfolioResponse?> GetPortfolioByAccountIdAsync(int accountId);

    Task<List<GetAllPortfolioResponse>> GetPortfoliosWithActiveStockCertificates(int userId);

    Task SellStockCertificateToPortfolio(Protos.SellStockRequest request);

    Task AddStockCertificateToPortfolio(AddStockCertificateToPortfolioRequest request);
}
