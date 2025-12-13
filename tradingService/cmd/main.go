package main

import (
	"log"
	"github.com/gofiber/fiber/v2"
	"tradingService/internal/controller"
	"tradingService/internal/repository/impl"
	impl2 "tradingService/internal/service/impl"
	"tradingService/internal/service"
	"tradingService/pkg/postgresql"
)

func main() {

	err := service.NewRabbitMqConnection("")
	if err != nil {
		log.Fatalf("Failed to initialize RabbitMQ: %v", err)
	}
	defer service.RabbitMQClient.Close()

	app := fiber.New()

	conn := postgresql.GetConnection()
	newTradeRepository := impl.NewTradeRepository(conn)
	newTradeService := impl2.NewTradeService(newTradeRepository)
	newTradeController := controller.NewTradeController(newTradeService)

	app.Post("/api/v1/trade/create", newTradeController.CreateTrade)
	app.Post("/api/v1/trade/direct/buy", newTradeController.DirectBuy)
	app.Post("/api/v1/trade/direct/sell", newTradeController.DirectSell)
	app.Get("/api/v1/trade/getTradeByAccount", newTradeController.GetTradeByAccount)
	app.Get("/api/v1/trade/getAllTrade", newTradeController.GetAllTrade)

	app.Listen(":8081")
}
