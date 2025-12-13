using Grpc.Core;
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
    private readonly IKafkaService _kafkaService; // Yeni eklendi

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
        // sadece ilgili user'ın portföylerini çek
        var portfolios = await _portfolioRepository
            .GetAllAsync(p => p.UserId == userId);

        // entity -> response mapping
        var responses = portfolios.Select(p => new GetAllPortfolioResponse
        {
            Id = p.Id,
            AccountId = p.AccountId,
            UserId = p.UserId,
         
        }).ToList();

        return responses;
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
        // Kullanıcıya ait portföyleri al, StockCertificate.Any(sc => sc.IsSell) olanlar
        var portfolios = await _portfolioRepository
            .GetAllAsync(p => p.UserId == userId && p.StockCertificates.Any(sc => sc.IsSell));

        // Map işlemi: her portföyün aktif StockCertificate’lerini GetAllPortfolioResponse listesine dönüştür
        var result = portfolios
            .SelectMany(p => p.StockCertificates
                .Where(sc => sc.IsSell) // sadece aktif hisseler
                .Select(sc => new GetAllPortfolioResponse
                {
                    Id = p.Id,
                    UserId = p.UserId,
                    AccountId = p.AccountId,
                    Symbol = sc.Symbol,
                    Lot = sc.Lot
                }))
            .ToList();

        return result;
    }


    public override async Task<SellStockResponse> SellStock(SellStockRequest request, ServerCallContext context)
    {
        SellStockResponse response = new();
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

    public async Task SellStockCertificateToPortfolio(SellStockRequest request)
    {
        var stockCertificate = await _stockCertificateRepository.GetAsync(sc => sc.Id == request.StockCertificateId && sc.PortfolioId == request.PortfolioId);

        if (stockCertificate == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Hisse senedi bulunamadı."));
        }

        if (stockCertificate.Lot < request.Lot)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Satılmak istenen lot miktarı mevcut lottan fazla olamaz."));
        }

        stockCertificate.Lot -= request.Lot;
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
            stockCertificate.Lot += request.Lot;
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
