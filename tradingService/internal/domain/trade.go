package domain

import "time"

type TradeType string
type TradeStatus string

const (
	Buy  TradeType = "BUY"
	Sell TradeType = "SELL"

	TradePending  TradeStatus = "PENDING"
	TradeExecuted TradeStatus = "EXECUTED"
	TradeFailed   TradeStatus = "FAILED"
	TradeCanceled TradeStatus = "CANCELED"
)

type Trade struct {
	ID         int         `gorm:"primaryKey" json:"id"`
	AccountID  int         `json:"accountId"`
	Symbol     string      `gorm:"size:20;not null" json:"symbol"`
	Type       TradeType   `gorm:"type:varchar(10);not null" json:"type"`
	Quantity   float64     `gorm:"not null" json:"quantity"`
	Price      float64     `gorm:"not null" json:"price"`
	Total      float64     `gorm:"-" json:"total"`
	ExecutedAt time.Time   `gorm:"autoCreateTime" json:"executedAt"`
	Status     TradeStatus `gorm:"type:varchar(20);default:'PENDING'" json:"status"`
}
