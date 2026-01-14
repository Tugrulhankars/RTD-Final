package controller

import (
	"encoding/base64"
	"encoding/json"
	"fmt"
	"github.com/gofiber/fiber/v2"
	"strings"
	"tradingService/internal/dto/request"
	"tradingService/internal/service"
)

// extractEmailFromJWT extracts email from JWT token's subject (sub) claim
func extractEmailFromJWT(token string) string {
	parts := strings.Split(token, ".")
	if len(parts) != 3 {
		return ""
	}

	payload := parts[1]
	// Add padding if needed
	if len(payload)%4 != 0 {
		payload += strings.Repeat("=", 4-len(payload)%4)
	}

	decoded, err := base64.RawURLEncoding.DecodeString(payload)
	if err != nil {
		// Try standard base64 decoding
		decoded, err = base64.StdEncoding.DecodeString(payload)
		if err != nil {
			fmt.Printf("DEBUG extractEmailFromJWT: Failed to decode JWT payload: %v\n", err)
			return ""
		}
	}

	var claims map[string]interface{}
	if err := json.Unmarshal(decoded, &claims); err != nil {
		fmt.Printf("DEBUG extractEmailFromJWT: Failed to unmarshal JWT claims: %v\n", err)
		return ""
	}

	// JWT subject is typically the email
	if email, ok := claims["sub"].(string); ok && email != "" {
		return email
	}

	return ""
}

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

func (t TradeController) DirectBuy(ctx *fiber.Ctx) error {
	var req request.DirectTradeRequest
	if err := ctx.BodyParser(&req); err != nil {
		return ctx.Status(fiber.StatusBadRequest).JSON(fiber.Map{
			"error": "invalid request body",
		})
	}

	// Try to get email from Authorization header (JWT token)
	if req.Email == "" {
		authHeader := ctx.Get("Authorization")
		if authHeader != "" && len(authHeader) > 7 && authHeader[:7] == "Bearer " {
			token := authHeader[7:]
			email := extractEmailFromJWT(token)
			if email != "" {
				req.Email = email
				fmt.Printf("DEBUG DirectBuy: Email extracted from JWT token: Email=%s\n", email)
			}
		}
	}

	res, err := t.tradeService.DirectBuy(ctx.Context(), req)
	if err != nil {
		return ctx.Status(fiber.StatusBadRequest).JSON(fiber.Map{
			"error": err.Error(),
		})
	}

	return ctx.Status(fiber.StatusCreated).JSON(res)
}

func (t TradeController) DirectSell(ctx *fiber.Ctx) error {
	var req request.DirectTradeRequest
	if err := ctx.BodyParser(&req); err != nil {
		return ctx.Status(fiber.StatusBadRequest).JSON(fiber.Map{
			"error": "invalid request body",
		})
	}

	// Try to get email from Authorization header (JWT token)
	if req.Email == "" {
		authHeader := ctx.Get("Authorization")
		if authHeader != "" && len(authHeader) > 7 && authHeader[:7] == "Bearer " {
			token := authHeader[7:]
			email := extractEmailFromJWT(token)
			if email != "" {
				req.Email = email
				fmt.Printf("DEBUG DirectSell: Email extracted from JWT token: Email=%s\n", email)
			}
		}
	}

	res, err := t.tradeService.DirectSell(ctx.Context(), req)
	if err != nil {
		return ctx.Status(fiber.StatusBadRequest).JSON(fiber.Map{
			"error": err.Error(),
		})
	}

	return ctx.Status(fiber.StatusCreated).JSON(res)
}

func (t TradeController) GetTradeHistoryByAccountId(ctx *fiber.Ctx) error {
	accountId := ctx.Params("accountId")
	if accountId == "" {
		return ctx.Status(fiber.StatusBadRequest).JSON(fiber.Map{
			"error": "accountId parameter is required",
		})
	}

	accountIdInt := 0
	if _, err := fmt.Sscanf(accountId, "%d", &accountIdInt); err != nil {
		return ctx.Status(fiber.StatusBadRequest).JSON(fiber.Map{
			"error": "invalid accountId",
		})
	}

	res, err := t.tradeService.GetTradeHistoryByAccountId(ctx.Context(), accountIdInt)
	if err != nil {
		return ctx.Status(fiber.StatusInternalServerError).JSON(fiber.Map{
			"error": err.Error(),
		})
	}

	return ctx.Status(fiber.StatusOK).JSON(fiber.Map{
		"success": true,
		"data":    res,
	})
}
