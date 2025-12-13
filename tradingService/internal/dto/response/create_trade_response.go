package response

type CreateTradeResponse struct {
	Message string `json:"message"`
	TradeId int32  `json:"tradeId"`
}
