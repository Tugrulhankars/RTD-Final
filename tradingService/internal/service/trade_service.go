package service

import (
	"context"
	"tradingService/internal/dto/request"
	"tradingService/internal/dto/response"
)

type TradeService interface {
	CreateTrade(ctx context.Context, request request.CreateTradeRequest) (response response.CreateTradeResponse, err error)
	// Doğrudan alım emri - AccountService ve MarketData/Price'a göre bakiye kontrolü yapar.
	DirectBuy(ctx context.Context, req request.DirectTradeRequest) (response response.CreateTradeResponse, error)
	// Doğrudan satım emri - PortfolioService'e göre kullanıcının ilgili hisseyi portföyünde bulundurup bulundurmadığını kontrol eder.
	DirectSell(ctx context.Context, req request.DirectTradeRequest) (response response.CreateTradeResponse, error)
	GetTradeByAccount(ctx context.Context, tradeId int) (response response.GetTradeResponse, err error)
	GetAllTrade(ctx context.Context) (response []response.GetTradeResponse, err error)
}
