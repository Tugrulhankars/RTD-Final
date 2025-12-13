package repository

import (
	"context"
	"tradingService/internal/domain"
)

type TradeRepository interface {
	Create(ctx context.Context, trade domain.Trade) error
	Update(ctx context.Context, trade domain.Trade) error
	Delete(ctx context.Context, tradeId int) error
	GetByAccount(ctx context.Context, id int) (*domain.Trade, error)
	GetAll(ctx context.Context) ([]*domain.Trade, error)
}
