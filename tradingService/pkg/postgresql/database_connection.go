package postgresql

import (
	"os"
	"gorm.io/driver/postgres"
	"gorm.io/gorm"
	"tradingService/internal/domain"
)

func GetConnection() *gorm.DB {
	host := os.Getenv("POSTGRES_HOST")
	if host == "" {
		host = "localhost"
	}
	
	user := os.Getenv("POSTGRES_USER")
	if user == "" {
		user = "metropol"
	}
	
	password := os.Getenv("POSTGRES_PASSWORD")
	if password == "" {
		password = "20002002"
	}
	
	dbname := os.Getenv("POSTGRES_DB")
	if dbname == "" {
		dbname = "trading_service"
	}

	dsn := "host=" + host + " user=" + user + " password=" + password + " dbname=" + dbname + " port=5432 sslmode=disable TimeZone=UTC"

	db, err := gorm.Open(postgres.Open(dsn), &gorm.Config{})
	if err != nil {
		panic(err)
	}

	err = db.AutoMigrate(&domain.Trade{})

	return db
}
