package events

import (
	"time"
	"tradingService/internal/domain"
)

type TradeCompletedEvent struct {
	AccountID  int              `json:"account_id"`
	Symbol     string           `json:"symbol"`
	Type       domain.TradeType `json:"trade_type"`
	Quantity   float64          `json:"quantity"`
	Price      float64          `json:"price"`
	Total      float64          `json:"total"`
	ExecutedAt time.Time        `json:"executed_at"`
	UserEmail  string           `json:"user_email"`
}
