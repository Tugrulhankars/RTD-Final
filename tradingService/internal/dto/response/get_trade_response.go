package response

import (
	"time"
	"tradingService/internal/domain"
)

type GetTradeResponse struct {
	Symbol     string             `json:"symbol"`
	Type       domain.TradeType   `json:"type"`
	Quantity   float64            `json:"quantity"`
	Price      float64            `json:"price"`
	Total      float64            `json:"total"`
	ExecutedAt time.Time          `json:"executed_at"`
	Status     domain.TradeStatus `json:"status"`
}
