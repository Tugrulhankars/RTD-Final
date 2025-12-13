package controller

import (
	"github.com/gofiber/fiber/v2"
	"tradingService/internal/dto/request"
	"tradingService/internal/service"
)

type TradeController struct {
	tradeService service.TradeService
}

func NewTradeController(tradeService service.TradeService) *TradeController {
	return &TradeController{tradeService: tradeService}
}

func (t TradeController) CreateTrade(ctx *fiber.Ctx) error {

	var req request.CreateTradeRequest

	if err := ctx.BodyParser(&req); err != nil {
		return ctx.Status(fiber.StatusBadRequest).JSON(fiber.Map{
			"error": "invalid request body",
		})
	}

	res, err := t.tradeService.CreateTrade(ctx.Context(), req)
	if err != nil {
		return ctx.Status(fiber.StatusInternalServerError).JSON(fiber.Map{})
	}
	return ctx.Status(fiber.StatusCreated).JSON(res)

}

func (t TradeController) GetTradeByAccount(ctx *fiber.Ctx) error {

	var req struct {
		AccountId int `json:"account_id"`
	}

	if err := ctx.BodyParser(&req); err != nil {
		return ctx.Status(fiber.StatusBadRequest).JSON(fiber.Map{
			"error": "invalid request body",
		})
	}

	res, err := t.tradeService.GetTradeByAccount(ctx.Context(), req.AccountId)
	if err != nil {
		return ctx.Status(fiber.StatusBadRequest).JSON(fiber.Map{"error": err.Error()})
	}

	return ctx.Status(fiber.StatusOK).JSON(res)

}

func (t TradeController) GetAllTrade(ctx *fiber.Ctx) error {

	res, err := t.tradeService.GetAllTrade(ctx.Context())
	if err != nil {
		return ctx.Status(fiber.StatusBadRequest).JSON(fiber.Map{"error": err.Error()})
	}

	return ctx.Status(fiber.StatusOK).JSON(res)

}

// DirectBuy
// Kullanıcının doğrudan alım emri göndermesi için endpoint.
// İş kuralları:
// - AccountService üzerinden yeterli bakiye kontrolü yapılır.
func (t TradeController) DirectBuy(ctx *fiber.Ctx) error {
	var req request.DirectTradeRequest
	if err := ctx.BodyParser(&req); err != nil {
		return ctx.Status(fiber.StatusBadRequest).JSON(fiber.Map{
			"error": "invalid request body",
		})
	}

	res, err := t.tradeService.DirectBuy(ctx.Context(), req)
	if err != nil {
		return ctx.Status(fiber.StatusBadRequest).JSON(fiber.Map{
			"error": err.Error(),
		})
	}

	return ctx.Status(fiber.StatusCreated).JSON(res)
}

// DirectSell
// Kullanıcının doğrudan satış emri göndermesi için endpoint.
// İş kuralları:
// - PortfolioService üzerinden ilgili hisseden yeterli lot kontrolü yapılır.
func (t TradeController) DirectSell(ctx *fiber.Ctx) error {
	var req request.DirectTradeRequest
	if err := ctx.BodyParser(&req); err != nil {
		return ctx.Status(fiber.StatusBadRequest).JSON(fiber.Map{
			"error": "invalid request body",
		})
	}

	res, err := t.tradeService.DirectSell(ctx.Context(), req)
	if err != nil {
		return ctx.Status(fiber.StatusBadRequest).JSON(fiber.Map{
			"error": err.Error(),
		})
	}

	return ctx.Status(fiber.StatusCreated).JSON(res)
}
