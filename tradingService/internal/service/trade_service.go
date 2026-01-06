package service

import (
	"context"
	"tradingService/internal/dto/request"
	"tradingService/internal/dto/response"
)

type TradeService interface {
	CreateTrade(ctx context.Context, request request.CreateTradeRequest) (response response.CreateTradeResponse, err error)
	DirectBuy(ctx context.Context, req request.DirectTradeRequest) (response response.CreateTradeResponse, err error)
	DirectSell(ctx context.Context, req request.DirectTradeRequest) (response response.CreateTradeResponse, err error)
	GetTradeByAccount(ctx context.Context, tradeId int) (response response.GetTradeResponse, err error)
	GetAllTrade(ctx context.Context) (response []response.GetTradeResponse, err error)
	GetTradeHistoryByAccountId(ctx context.Context, accountId int) (response []response.GetTradeResponse, err error)
}
