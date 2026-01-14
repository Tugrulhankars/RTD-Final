package request

type DirectTradeRequest struct {
	UserID    int     `json:"user_id"`
	AccountID int     `json:"account_id"`
	Symbol    string  `json:"symbol"`
	Quantity  float64 `json:"quantity"`
	Price     float64 `json:"price"`
	Side      string  `json:"side"`
	Email     string  `json:"email,omitempty"` // Optional: Email from frontend or JWT
}
