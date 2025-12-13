package request

import "tradingService/internal/domain"

type CreateTradeRequest struct {
	AccountID int              `json:"account_id"`
	Symbol    string           `json:"symbol"`
	Type      domain.TradeType `json:"trade_type"`
	Quantity  float64          `json:"quantity"`
	Price     float64          `json:"price"`
	Total     float64          `json:"total"`
}
