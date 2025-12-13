package postgresql

import (
	"gorm.io/driver/postgres"
	"gorm.io/gorm"
	"tradingService/internal/domain"
)

func GetConnection() *gorm.DB {

	dsn := "host=localhost user=metropol password=20002002 dbname=trading_service port=5432 sslmode=disable TimeZone=Europe/Istanbul"

	db, err := gorm.Open(postgres.Open(dsn), &gorm.Config{})
	if err != nil {
		panic(err)
	}

	err = db.AutoMigrate(&domain.Trade{})

	return db
}
