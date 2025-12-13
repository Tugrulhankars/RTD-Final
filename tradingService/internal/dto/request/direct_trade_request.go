package request

// DirectTradeRequest
// Kullanıcının doğrudan alım/satım emri için kullanacağı DTO.
// UserID, AccountID ile birlikte gelir; AccountService ve PortfolioService
// ile iş kuralları kontrol edildikten sonra normal CreateTrade akışı kullanılır.
type DirectTradeRequest struct {
	UserID    int     `json:"user_id"`
	AccountID int     `json:"account_id"`
	Symbol    string  `json:"symbol"`
	Quantity  float64 `json:"quantity"`
	Price     float64 `json:"price"` // İsteğe bağlı; 0 ise frontend MarketDataService'ten besleyebilir
	Side      string  `json:"side"`  // "BUY" veya "SELL"
}


