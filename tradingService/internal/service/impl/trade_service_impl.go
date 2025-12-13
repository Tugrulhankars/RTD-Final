package impl

import (
	"context"
	"encoding/json"
	"errors"
	"fmt"
	"net/http"
	"time"
	"tradingService/internal/domain"
	"tradingService/internal/dto/request"
	"tradingService/internal/dto/response"
	"tradingService/internal/events"
	"tradingService/internal/repository"
	"tradingService/internal/service"
	pb "tradingService/internal/tradeService"
)

type GrpcTradeServer struct {
	pb.UnimplementedTradeServiceServer
	tradeService service.TradeService
}

type TradeService struct {
	tradeRepository repository.TradeRepository
}

func NewTradeService(tradeRepository repository.TradeRepository) service.TradeService {
	return &TradeService{tradeRepository: tradeRepository}
}

// Basit config - istersen ileride env/config'e taşıyabilirsin
const (
	accountServiceBaseURL   = "http://localhost:5239/api/account"
	portfolioServiceBaseURL = "http://localhost:5242/api/portfolio"
)

type accountInfo struct {
	AccountId int     `json:"accountId"`
	UserId    int     `json:"userId"`
	Balance   float64 `json:"balance"`
}

type portfolioStock struct {
	Symbol string  `json:"symbol"`
	Lot    float64 `json:"lot"`
}

// DirectBuy
// Kullanıcının doğrudan alım emri vermesi için iş kuralları:
// - AccountService üzerinden kullanıcının bakiyesi alınır.
// - Bakiye, price * quantity tutarını karşılamalıdır.
func (t TradeService) DirectBuy(ctx context.Context, req request.DirectTradeRequest) (response.CreateTradeResponse, error) {
	if req.Price <= 0 || req.Quantity <= 0 {
		return response.CreateTradeResponse{}, errors.New("price ve quantity pozitif olmalıdır")
	}

	total := req.Price * req.Quantity

	// Hesap bakiyesini AccountService'ten al
	acc, err := getAccountByUser(req.UserID)
	if err != nil {
		return response.CreateTradeResponse{}, fmt.Errorf("hesap bilgisi alınamadı: %w", err)
	}

	if acc.AccountId != 0 && acc.AccountId != req.AccountID {
		// Güvenlik açısından accountId eşleşmesini de kontrol et
		return response.CreateTradeResponse{}, errors.New("accountId ve userId eşleşmiyor")
	}

	if acc.Balance < total {
		return response.CreateTradeResponse{}, fmt.Errorf("yetersiz bakiye. Gerekli: %.2f, Mevcut: %.2f", total, acc.Balance)
	}

	// Kurallar sağlandı, normal CreateTrade akışını kullan
	createReq := request.CreateTradeRequest{
		AccountID: req.AccountID,
		Symbol:    req.Symbol,
		Type:      domain.TradeTypeBuy,
		Quantity:  req.Quantity,
		Price:     req.Price,
		Total:     total,
	}

	return t.CreateTrade(ctx, createReq)
}

// DirectSell
// Kullanıcının doğrudan satış emri vermesi için iş kuralları:
// - PortfolioService üzerinden kullanıcının aktif hisseleri alınır.
// - İlgili sembolde yeterli lot bulunmalıdır.
func (t TradeService) DirectSell(ctx context.Context, req request.DirectTradeRequest) (response.CreateTradeResponse, error) {
	if req.Price <= 0 || req.Quantity <= 0 {
		return response.CreateTradeResponse{}, errors.New("price ve quantity pozitif olmalıdır")
	}

	// Portföyde ilgili hisse var mı ve yeterli lot var mı kontrol et
	activeStocks, err := getActiveStocksByUser(req.UserID)
	if err != nil {
		return response.CreateTradeResponse{}, fmt.Errorf("portföy bilgisi alınamadı: %w", err)
	}

	var lot float64
	for _, s := range activeStocks {
		if s.Symbol == req.Symbol {
			lot = s.Lot
			break
		}
	}

	if lot <= 0 {
		return response.CreateTradeResponse{}, fmt.Errorf("portföyde %s sembolü bulunmuyor", req.Symbol)
	}

	if lot < req.Quantity {
		return response.CreateTradeResponse{}, fmt.Errorf("portföyde yeterli lot yok. İstenen: %.2f, Mevcut: %.2f", req.Quantity, lot)
	}

	total := req.Price * req.Quantity

	// Kurallar sağlandı, normal CreateTrade akışını kullan
	createReq := request.CreateTradeRequest{
		AccountID: req.AccountID,
		Symbol:    req.Symbol,
		Type:      domain.TradeTypeSell,
		Quantity:  req.Quantity,
		Price:     req.Price,
		Total:     total,
	}

	return t.CreateTrade(ctx, createReq)
}

// AccountService: /api/account/getAccountByUser/{userId}
func getAccountByUser(userID int) (accountInfo, error) {
	url := fmt.Sprintf("%s/getAccountByUser/%d", accountServiceBaseURL, userID)
	resp, err := http.Get(url)
	if err != nil {
		return accountInfo{}, err
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		return accountInfo{}, fmt.Errorf("account service status code: %d", resp.StatusCode)
	}

	var acc accountInfo
	if err := json.NewDecoder(resp.Body).Decode(&acc); err != nil {
		return accountInfo{}, err
	}
	return acc, nil
}

// PortfolioService: /api/portfolio/getActiveStocksByUser/{userId}
func getActiveStocksByUser(userID int) ([]portfolioStock, error) {
	url := fmt.Sprintf("%s/getActiveStocksByUser/%d", portfolioServiceBaseURL, userID)
	resp, err := http.Get(url)
	if err != nil {
		return nil, err
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		return nil, fmt.Errorf("portfolio service status code: %d", resp.StatusCode)
	}

	var stocks []portfolioStock
	if err := json.NewDecoder(resp.Body).Decode(&stocks); err != nil {
		return nil, err
	}
	return stocks, nil
}

func (s *GrpcTradeServer) CreateTradeGrpc(ctx context.Context, req *pb.CreateTradeRequest) (*pb.CreateTradeResponse, error) {

	tradeRequest := request.CreateTradeRequest{
		AccountID: int(req.AccountId),
		Symbol:    req.Symbol,
		Type:      domain.TradeType(req.Type), // domain enum mapping yapabilirsiniz
		Quantity:  float64(req.Quantity),
		Price:     float64(req.Price),
		Total:     float64(req.Price) * float64(req.Quantity), // toplamı hesaplıyoruz
	}

	// Trade oluştur
	tradeResponse, err := s.tradeService.CreateTrade(ctx, tradeRequest)
	if err != nil {
		return nil, err
	}

	return &pb.CreateTradeResponse{
		Message: tradeResponse.Message,
		TradeId: tradeResponse.TradeId,
	}, nil
}

func (t TradeService) CreateTrade(ctx context.Context, request request.CreateTradeRequest) (res response.CreateTradeResponse, err error) {

	trade := &domain.Trade{
		Price:      request.Price,
		Quantity:   request.Quantity,
		Symbol:     request.Symbol,
		Status:     "",
		Total:      request.Total,
		ExecutedAt: time.Now(),
		AccountID:  request.AccountID,
		Type:       request.Type,
	}
	err = t.tradeRepository.Create(ctx, *trade)
	if err != nil {
		return response.CreateTradeResponse{}, err
	}

	// GORM Create işleminden sonra trade.ID otomatik olarak set edilir
	res.TradeId = trade.ID
	res.Message = "Trade created successfully"

	event := &events.TradeCompletedEvent{
		Type:       trade.Type,
		Symbol:     trade.Symbol,
		Price:      trade.Price,
		Quantity:   trade.Quantity,
		Total:      trade.Total,
		AccountID:  trade.AccountID,
		ExecutedAt: trade.ExecutedAt,
	}
	event_err := service.RabbitMQClient.SendTradeCompletedEvent(event)
	if event_err != nil {
		// Event gönderilemese bile trade oluşturuldu, response döndürüyoruz
		return res, nil
	}

	return res, nil
}

func (t TradeService) GetTradeByAccount(ctx context.Context, tradeId int) (tradeResponse response.GetTradeResponse, err error) {
	trade, err := t.tradeRepository.GetByAccount(ctx, tradeId)
	if err != nil {
		return tradeResponse, err
	}

	tradeResponse.Status = trade.Status
	tradeResponse.Total = trade.Total
	tradeResponse.Symbol = trade.Symbol
	tradeResponse.Price = trade.Price
	tradeResponse.Quantity = trade.Quantity
	tradeResponse.ExecutedAt = trade.ExecutedAt
	tradeResponse.Type = trade.Type

	return tradeResponse, nil

}

func (t TradeService) GetAllTrade(ctx context.Context) (responses []response.GetTradeResponse, err error) {

	trades, err := t.tradeRepository.GetAll(ctx)
	if err != nil {
		return nil, err
	}

	responses = make([]response.GetTradeResponse, len(trades))

	for i, trade := range trades {
		responses[i] = response.GetTradeResponse{
			Status:     trade.Status,
			Total:      trade.Total,
			Symbol:     trade.Symbol,
			Price:      trade.Price,
			Quantity:   trade.Quantity,
			ExecutedAt: trade.ExecutedAt,
			Type:       trade.Type,
		}
	}

	return responses, nil
}
