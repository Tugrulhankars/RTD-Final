package service

import (
	"encoding/json"
	"fmt"
	"github.com/rabbitmq/amqp091-go"
	"log"
	"os"
	"tradingService/internal/events"
)

var RabbitMQClient *RabbitMQ

type RabbitMQ struct {
	Conn    *amqp091.Connection
	Channel *amqp091.Channel
}

func NewRabbitMqConnection(amqpURL string) error {
	if amqpURL == "" {
		amqpURL = os.Getenv("AMQP_URL")
		if amqpURL == "" {
			log.Fatal("AMQP_URL environment variable is not set")
		}
	}

	conn, err := amqp091.Dial(amqpURL)
	if err != nil {
		return fmt.Errorf("failed to connect to RabbitMQ: %v", err)
	}

	ch, err := conn.Channel()
	if err != nil {
		conn.Close()
		return fmt.Errorf("failed to open a channel: %v", err)
	}

	RabbitMQClient = &RabbitMQ{
		Conn:    conn,
		Channel: ch,
	}

	log.Println("RabbitMQ connection established successfully")
	return nil
}

func (r *RabbitMQ) SendTradeCompletedEvent(tradeEvent *events.TradeCompletedEvent) error {
	if r.Channel == nil {
		return fmt.Errorf("RabbitMQ channel is not initialized")
	}

	queueName := "trade_completed_notifications"
	q, err := r.Channel.QueueDeclare(
		queueName,
		true,
		false,
		false,
		false,
		nil,
	)

	if err != nil {
		return fmt.Errorf("failed to declare a queue: %v", err)
	}

	body, err := json.Marshal(tradeEvent)
	if err != nil {
		return fmt.Errorf("failed to marshal trade event: %v", err)
	}

	err = r.Channel.Publish(
		"",
		q.Name,
		false,
		false,
		amqp091.Publishing{
			ContentType: "application/json",
			Body:        body,
		})
	if err != nil {
		return fmt.Errorf("failed to publish message: %v", err)
	}

	log.Printf("Trade completed event sent to RabbitMQ queue '%s': Symbol=%s, Type=%s, Price=%.2f, Quantity=%.2f, ExecutedAt=%s",
		queueName, tradeEvent.Symbol, tradeEvent.Type, tradeEvent.Price, tradeEvent.Quantity, tradeEvent.ExecutedAt.Format("2006-01-02 15:04:05"))

	return nil
}

func (r *RabbitMQ) Close() error {
	if r.Channel != nil {
		r.Channel.Close()
	}
	if r.Conn != nil {
		return r.Conn.Close()
	}
	return nil
}
