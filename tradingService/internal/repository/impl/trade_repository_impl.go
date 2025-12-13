package impl

import (
	"context"
	"gorm.io/gorm"
	"tradingService/internal/domain"
	"tradingService/internal/repository"
)

type TradeRepository struct {
	db *gorm.DB
}

func NewTradeRepository(db *gorm.DB) repository.TradeRepository {
	return &TradeRepository{db: db}
}

func (t TradeRepository) Create(ctx context.Context, trade domain.Trade) error {
	err := t.db.WithContext(ctx).Create(&trade).Error
	if err != nil {
		return err
	}
	return nil
}

func (t TradeRepository) Update(ctx context.Context, trade domain.Trade) error {
	err := t.db.WithContext(ctx).Save(trade).Error
	if err != nil {
		return err
	}
	return nil

}

func (t TradeRepository) Delete(ctx context.Context, tradeId int) error {
	err := t.db.WithContext(ctx).Delete(&domain.Trade{}, tradeId).Error
	if err != nil {
		return err
	}
	return nil

}

func (t TradeRepository) GetByAccount(ctx context.Context, id int) (*domain.Trade, error) {
	var trade domain.Trade
	err := t.db.WithContext(ctx).First(&trade, id).Error
	if err != nil {
		return nil, err
	}

	return &trade, nil

}

func (t TradeRepository) GetAll(ctx context.Context) ([]*domain.Trade, error) {
	var trades []*domain.Trade

	err := t.db.WithContext(ctx).Find(&trades).Error
	if err != nil {
		return nil, err
	}
	return trades, nil

}
