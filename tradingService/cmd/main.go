package main

import (
	"log"
	"net"
	"os"
	"sync"
	"time"
	"github.com/gofiber/fiber/v2"
	"google.golang.org/grpc"
	"tradingService/internal/controller"
	"tradingService/internal/repository/impl"
	impl2 "tradingService/internal/service/impl"
	"tradingService/internal/service"
	"tradingService/pkg/postgresql"
	pb "tradingService/internal/tradeService"
)

func main() {
	err := service.NewRabbitMqConnection("")
	if err != nil {
		log.Printf("WARNING: Failed to initialize RabbitMQ: %v. Application will continue without RabbitMQ.", err)
	} else {
		defer service.RabbitMQClient.Close()
	}

	conn := postgresql.GetConnection()
	newTradeRepository := impl.NewTradeRepository(conn)
	newTradeService := impl2.NewTradeService(newTradeRepository)
	newTradeController := controller.NewTradeController(newTradeService)

	// HTTP REST API (Fiber)
	app := fiber.New()
	app.Post("/api/v1/trade/create", newTradeController.CreateTrade)
	app.Post("/api/v1/trade/direct/buy", newTradeController.DirectBuy)
	app.Post("/api/v1/trade/direct/sell", newTradeController.DirectSell)
	app.Get("/api/v1/trade/getTradeByAccount", newTradeController.GetTradeByAccount)
	app.Get("/api/v1/trade/getAllTrade", newTradeController.GetAllTrade)
	app.Get("/api/trade/history/:accountId", newTradeController.GetTradeHistoryByAccountId)

	// gRPC Server
	grpcPort := os.Getenv("GRPC_PORT")
	if grpcPort == "" {
		grpcPort = "5003" // Default gRPC port for TradeService
	}
	
	lis, err := net.Listen("tcp", ":"+grpcPort)
	if err != nil {
		log.Fatalf("Failed to listen on gRPC port %s: %v", grpcPort, err)
	}

	grpcServer := grpc.NewServer()
	grpcTradeServer := &impl2.GrpcTradeServer{
		TradeService: newTradeService,
	}
	pb.RegisterTradeServiceServer(grpcServer, grpcTradeServer)

	var wg sync.WaitGroup
	wg.Add(2)

	// Start HTTP server
	go func() {
		defer wg.Done()
		log.Printf("HTTP REST API server starting on :9084")
		if err := app.Listen(":9084"); err != nil {
			log.Fatalf("Failed to start HTTP server: %v", err)
		}
	}()

	// Start gRPC server
	go func() {
		defer wg.Done()
		log.Printf("gRPC server starting on :%s (listening for connections...)", grpcPort)
		if err := grpcServer.Serve(lis); err != nil {
			log.Fatalf("Failed to start gRPC server: %v", err)
		}
	}()

	// Give servers a moment to start
	time.Sleep(500 * time.Millisecond)
	log.Printf("TradeService started successfully - HTTP: :9084, gRPC: :%s", grpcPort)
	wg.Wait()
}
