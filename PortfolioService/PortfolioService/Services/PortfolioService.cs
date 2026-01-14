using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using PortfolioService.Dtos.Request;
using PortfolioService.Dtos.Response;
using PortfolioService.Models;
using PortfolioService.Protos;
using PortfolioService.Repositories.Abstracts;
using PortfolioService.Services.Abstracts;
using static PortfolioService.Protos.PortfolioService;

namespace PortfolioService.Services;

public class PortfolioService : PortfolioServiceBase,IPortfolioService
{
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly IStockCertificateRepository _stockCertificateRepository; 
    private readonly IKafkaService _kafkaService;

    public PortfolioService(IPortfolioRepository portfolioRepository, IStockCertificateRepository stockCertificateRepository, IKafkaService kafkaService)
    {
        _portfolioRepository = portfolioRepository;
        _stockCertificateRepository = stockCertificateRepository;
        _kafkaService = kafkaService;
    }
    public async Task CreatePortfolio(CreatePortfolioRequest request)
    {
        Portfolio portfolio=new Portfolio()
        {
            UserId=request.UserId,
            AccountId=request.AccountId,

        };
        await _portfolioRepository.AddAsync(portfolio);
       
    }

    public async Task<List<GetAllPortfolioResponse>> GetAllPortfolioByUserAsync(int userId)
    {
        try
        {
            var portfolios = await _portfolioRepository
                .GetAllAsync(p => p.UserId == userId);

            var responses = portfolios.Select(p => new GetAllPortfolioResponse
            {
                Id = p.Id,
                AccountId = p.AccountId,
                UserId = p.UserId,
             
            }).ToList();

            return responses;
        }
        catch (Exception ex)
        {
            // Veritabanı bağlantı hatası durumunda boş liste döndür
            if (ex is SqlException || 
                (ex.InnerException is SqlException) ||
                ex.Message.Contains("network-related", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("instance-specific", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("server was not found", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("could not open a connection", StringComparison.OrdinalIgnoreCase))
            {
                return new List<GetAllPortfolioResponse>();
            }
            throw;
        }
    }

    public async Task<GetAllPortfolioResponse?> GetPortfolioByAccountIdAsync(int accountId)
    {
        try
        {
            var portfolio = await _portfolioRepository
                .GetAsync(p => p.AccountId == accountId);

            if (portfolio == null)
            {
                return null;
            }

            return new GetAllPortfolioResponse
            {
                Id = portfolio.Id,
                AccountId = portfolio.AccountId,
                UserId = portfolio.UserId,
            };
        }
        catch (Exception ex)
        {
            // Veritabanı bağlantı hatası durumunda null döndür
            if (ex is SqlException || 
                (ex.InnerException is SqlException) ||
                ex.Message.Contains("network-related", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("instance-specific", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("server was not found", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("could not open a connection", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            throw;
        }
    }

    public async Task<bool> HasStockInPortfolioAsync(int userId, string symbol)
    {
        var portfolio = await _portfolioRepository
            .GetAsync(p => p.UserId == userId && p.StockCertificates.Any(sc => sc.Symbol==symbol));
        return portfolio != null;
    }

    public override async Task<HasStockInPortfolioResponse> HasStockInPortfolio(HasStockInPortfolioRequest request, ServerCallContext context)
    {
        HasStockInPortfolioResponse response = new();
        bool hasStockInPortfolio =await HasStockInPortfolioAsync(request.PortfolioId,request.Symbol);
        if (hasStockInPortfolio)
        {
            response.Result= true;
        }
        else
        {
            response.Result = false;
        }
        
        return response;
    }

    public async Task<List<GetAllPortfolioResponse>> GetPortfoliosWithActiveStockCertificates(int userId)
    {
        try
        {
            var portfolios = await _portfolioRepository
                .GetAllAsync(
                    predicate: p => p.UserId == userId,
                    include: query => query.Include(p => p.StockCertificates)
                );

            if (portfolios == null || !portfolios.Any())
            {
                return new List<GetAllPortfolioResponse>();
            }

            var result = portfolios
                .Where(p => p.StockCertificates != null && p.StockCertificates.Any(sc => !sc.IsSell))
                .SelectMany(p => p.StockCertificates!
                    .Where(sc => !sc.IsSell)
                    .Select(sc => new GetAllPortfolioResponse
                    {
                        Id = p.Id,
                        UserId = p.UserId,
                        AccountId = p.AccountId,
                        Symbol = sc.Symbol,
                        Lot = sc.Lot,
                        AveragePrice = sc.PricePerShare
                    }))
                .ToList();

            return result;
        }
        catch (Exception ex)
        {
            // Veritabanı bağlantı hatası durumunda boş liste döndür
            if (ex is SqlException || 
                (ex.InnerException is SqlException) ||
                ex.Message.Contains("network-related", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("instance-specific", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("server was not found", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("could not open a connection", StringComparison.OrdinalIgnoreCase))
            {
                return new List<GetAllPortfolioResponse>();
            }
            throw;
        }
    }

    public override async Task<Protos.SellStockResponse> SellStock(Protos.SellStockRequest request, ServerCallContext context)
    {
        Protos.SellStockResponse response = new();
        try
        {
            await SellStockCertificateToPortfolio(request);
            response.Success = true;
            response.Message = "Hisse senedi başarıyla satıldı.";
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Message = $"Hisse senedi satılırken bir hata oluştu: {ex.Message}";
        }
        return response;
    }

    public async Task SellStockCertificateToPortfolio(Protos.SellStockRequest request)
    {
        var stockCertificate = request.StockCertificateId > 0
            ? await _stockCertificateRepository.GetAsync(sc => sc.Id == request.StockCertificateId && sc.PortfolioId == request.PortfolioId)
            : await _stockCertificateRepository.GetAsync(sc => sc.PortfolioId == request.PortfolioId && sc.Symbol == request.Symbol && !sc.IsSell);

        if (stockCertificate == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Hisse senedi bulunamadı."));
        }

        if (stockCertificate.Lot < request.Lot)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Satılmak istenen lot miktarı mevcut lottan fazla olamaz."));
        }

        stockCertificate.Lot -= (int)request.Lot;
        if (stockCertificate.Lot == 0)
        {
            stockCertificate.IsSell = true;
            stockCertificate.SellDate = DateTime.UtcNow;
        }
        await _stockCertificateRepository.UpdateAsync(stockCertificate);
    }
    public override async Task<AddStockCertificateToPortfolioResponse> AddStockCertificateToPortfolio(AddStockCertificateToPortfolioRequest request, ServerCallContext context)
    {
        AddStockCertificateToPortfolioResponse response = new();
        try
        {
            await AddStockCertificateToPortfolio(request);
            response.Success = true;
            response.Message = "Hisse senedi başarıyla portföye eklendi.";
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Message = $"Hisse senedi eklenirken bir hata oluştu: {ex.Message}";
        }
        return response;
    }
    public async Task AddStockCertificateToPortfolio(AddStockCertificateToPortfolioRequest request)
    {
        var portfolio = await _portfolioRepository.GetAsync(p => p.Id == request.PortfolioId);

        if (portfolio == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Portföy bulunamadı."));
        }

        var stockCertificate = await _stockCertificateRepository.GetAsync(sc => sc.PortfolioId == request.PortfolioId && sc.Symbol == request.Symbol && !sc.IsSell);

        if (stockCertificate != null)
        {
            var oldTotalCost = stockCertificate.PricePerShare * stockCertificate.Lot;
            var newTotalCost = request.PricePerShare * request.Lot;
            var newTotalLot = stockCertificate.Lot + request.Lot;
            
            var newAveragePrice = (oldTotalCost + newTotalCost) / newTotalLot;
            
            stockCertificate.Lot = newTotalLot;
            stockCertificate.PricePerShare = newAveragePrice;
            await _stockCertificateRepository.UpdateAsync(stockCertificate);
        }
        else
        {
            var newStockCertificate = new StockCertificate
            {
                PortfolioId = request.PortfolioId,
                Symbol = request.Symbol,
                Lot = request.Lot,
                PricePerShare = request.PricePerShare,
                BuyDate = DateTime.UtcNow,
                IsSell = false
            };
            await _stockCertificateRepository.AddAsync(newStockCertificate);
        }
    }
}
