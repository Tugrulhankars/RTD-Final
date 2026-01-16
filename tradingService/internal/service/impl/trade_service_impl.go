package impl

import (
	"bytes"
	"context"
	"encoding/json"
	"errors"
	"fmt"
	"io"
	"net/http"
	"time"
	"tradingService/internal/domain"
	"tradingService/internal/dto/request"
	"tradingService/internal/dto/response"
	"tradingService/internal/events"
	"tradingService/internal/repository"
	"tradingService/internal/service"
	pb "tradingService/internal/tradeService"
)

type GrpcTradeServer struct {
	pb.UnimplementedTradeServiceServer
	TradeService service.TradeService
}

type TradeService struct {
	tradeRepository repository.TradeRepository
}

func NewTradeService(tradeRepository repository.TradeRepository) service.TradeService {
	return &TradeService{tradeRepository: tradeRepository}
}

const (
	accountServiceBaseURL   = "http://localhost:5239/api/account"
	portfolioServiceBaseURL = "http://localhost:5242/api/portfolio"
	authUserServiceBaseURL  = "http://localhost:8080/api/v1/users"
)

type accountInfo struct {
	AccountId int     `json:"accountId"`
	UserId    int     `json:"userId"`
	Balance   float64 `json:"balance"`
	Email     string  `json:"email"`
}

type userInfo struct {
	Id    int64  `json:"id"`
	Email string `json:"email"`
}

type portfolioStock struct {
	Symbol string  `json:"symbol"`
	Lot    float64 `json:"lot"`
}

func (t TradeService) DirectBuy(ctx context.Context, req request.DirectTradeRequest) (response.CreateTradeResponse, error) {
	if req.Price <= 0 || req.Quantity <= 0 {
		return response.CreateTradeResponse{}, errors.New("price ve quantity pozitif olmalıdır")
	}

	total := req.Price * req.Quantity

	fmt.Printf("STEP A: Getting account information for UserID=%d\n", req.UserID)
	acc, err := getAccountByUser(req.UserID)
	if err != nil {
		return response.CreateTradeResponse{}, fmt.Errorf("hesap bilgisi alınamadı: %w", err)
	}

	actualAccountID := acc.AccountId
	if actualAccountID == 0 {
		actualAccountID = req.AccountID
		fmt.Printf("WARNING: AccountId from Account Service is 0, using request AccountID=%d\n", req.AccountID)
	}

	if acc.AccountId != 0 && acc.AccountId != req.AccountID {
		return response.CreateTradeResponse{}, fmt.Errorf("accountId mismatch: Account Service returned AccountId=%d but request has AccountID=%d", acc.AccountId, req.AccountID)
	}

	fmt.Printf("STEP A COMPLETE: UserID=%d, AccountID=%d (from Account Service), Balance=%.2f, Email='%s' (length=%d)\n", req.UserID, actualAccountID, acc.Balance, acc.Email, len(acc.Email))

	if acc.Balance < total {
		return response.CreateTradeResponse{}, fmt.Errorf("yetersiz bakiye. Gerekli: %.2f, Mevcut: %.2f", total, acc.Balance)
	}

	createReq := request.CreateTradeRequest{
		AccountID: actualAccountID,
		Symbol:    req.Symbol,
		Type:      domain.Buy,
		Quantity:  req.Quantity,
		Price:     req.Price,
		Total:     total,
	}

	tradeRes, err := t.CreateTrade(ctx, createReq)
	if err != nil {
		return response.CreateTradeResponse{}, err
	}

	fmt.Printf("STEP B: Trade created successfully. TradeID=%d\n", tradeRes.TradeId)

	fmt.Printf("STEP C: Updating account balance for AccountID=%d, Amount=%.2f\n", actualAccountID, -total)
	updateBalanceErr := updateAccountBalance(actualAccountID, req.UserID, -total)
	if updateBalanceErr != nil {
		fmt.Printf("ERROR: Account balance update failed for AccountID=%d, UserID=%d, Amount=%.2f: %v\n", 
			actualAccountID, req.UserID, -total, updateBalanceErr)
	} else {
		fmt.Printf("STEP C COMPLETE: Account balance updated successfully for AccountID=%d, Amount=%.2f\n", actualAccountID, -total)
	}

	fmt.Printf("STEP D: Adding stock to portfolio. UserID=%d, AccountID=%d, Symbol=%s, Lot=%.2f\n", 
		req.UserID, actualAccountID, req.Symbol, req.Quantity)
	portfolioErr := addStockToPortfolio(req.UserID, actualAccountID, req.Symbol, req.Quantity, req.Price)
	if portfolioErr != nil {
		fmt.Printf("ERROR: Portfolio update failed for UserID=%d, AccountID=%d, Symbol=%s, Lot=%.2f: %v\n", 
			req.UserID, req.AccountID, req.Symbol, req.Quantity, portfolioErr)
	} else {
		fmt.Printf("SUCCESS: Stock added to portfolio for UserID=%d, Symbol=%s, Lot=%.2f\n", 
			req.UserID, req.Symbol, req.Quantity)
	}

	// Get user email and send trade completed event
	fmt.Printf("STEP E: Getting user email for notification. UserID=%d, RequestEmail=%s, AccountEmail from STEP A=%s\n", req.UserID, req.Email, acc.Email)
	userEmail := ""
	
	// Priority 1: Use email from request (from JWT token or frontend)
	if req.Email != "" {
		userEmail = req.Email
		fmt.Printf("STEP E COMPLETE: User email retrieved from request (JWT/frontend): UserID=%d, Email=%s\n", req.UserID, userEmail)
	} else if acc.Email != "" {
		// Priority 2: Use email from account (which we already have)
		userEmail = acc.Email
		fmt.Printf("STEP E COMPLETE: User email retrieved from account: UserID=%d, Email=%s\n", req.UserID, userEmail)
	} else {
		fmt.Printf("STEP E WARNING: Email not found in request or account. Attempting fallback - getting email from AccountService by AccountId=%d\n", actualAccountID)
		
		// Priority 3: Try to get account by AccountId to get email
		accByID, accByIDErr := getAccountByAccountId(actualAccountID)
		if accByIDErr == nil && accByID.Email != "" {
			userEmail = accByID.Email
			fmt.Printf("STEP E COMPLETE: User email retrieved from AccountService by AccountId: AccountId=%d, Email=%s\n", actualAccountID, userEmail)
		} else {
			accByIDEmail := ""
			if accByIDErr == nil {
				accByIDEmail = accByID.Email
			}
			fmt.Printf("STEP E WARNING: Failed to get user email from all sources. UserID=%d, AccountId=%d, RequestEmail=%s, AccountEmail(from UserId)=%s, AccountEmail(from AccountId)=%s\n", 
				req.UserID, actualAccountID, req.Email, acc.Email, accByIDEmail)
		}
	}

	// Send trade completed event with email
	fmt.Printf("STEP F: Sending trade completed event. Symbol=%s, Type=BUY, Email=%s\n", req.Symbol, userEmail)
	event := &events.TradeCompletedEvent{
		Type:       domain.Buy,
		Symbol:     req.Symbol,
		Price:      req.Price,
		Quantity:   req.Quantity,
		Total:      total,
		AccountID:  actualAccountID,
		ExecutedAt: time.Now(),
		UserEmail:  userEmail,
	}
	eventErr := service.RabbitMQClient.SendTradeCompletedEvent(event)
	if eventErr != nil {
		fmt.Printf("STEP F ERROR: Failed to send trade completed event: %v\n", eventErr)
	} else {
		fmt.Printf("STEP F COMPLETE: Trade completed event sent successfully: Symbol=%s, Type=BUY, Email=%s\n", req.Symbol, userEmail)
	}

	return tradeRes, nil
}

func (t TradeService) DirectSell(ctx context.Context, req request.DirectTradeRequest) (response.CreateTradeResponse, error) {
	if req.Price <= 0 || req.Quantity <= 0 {
		return response.CreateTradeResponse{}, errors.New("price ve quantity pozitif olmalıdır")
	}

	activeStocks, err := getActiveStocksByUser(req.UserID)
	if err != nil {
		return response.CreateTradeResponse{}, fmt.Errorf("portföy bilgisi alınamadı: %w", err)
	}

	var lot float64
	for _, s := range activeStocks {
		if s.Symbol == req.Symbol {
			lot = s.Lot
			break
		}
	}

	if lot <= 0 {
		return response.CreateTradeResponse{}, fmt.Errorf("portföyde %s sembolü bulunmuyor", req.Symbol)
	}

	if lot < req.Quantity {
		return response.CreateTradeResponse{}, fmt.Errorf("portföyde yeterli lot yok. İstenen: %.2f, Mevcut: %.2f", req.Quantity, lot)
	}

	total := req.Price * req.Quantity

	createReq := request.CreateTradeRequest{
		AccountID: req.AccountID,
		Symbol:    req.Symbol,
		Type:      domain.Sell,
		Quantity:  req.Quantity,
		Price:     req.Price,
		Total:     total,
	}

	tradeRes, err := t.CreateTrade(ctx, createReq)
	if err != nil {
		return response.CreateTradeResponse{}, err
	}

	updateBalanceErr := updateAccountBalance(req.AccountID, req.UserID, total)
	if updateBalanceErr != nil {
		fmt.Printf("WARNING: Account balance update failed: %v\n", updateBalanceErr)
	}

	portfolioErr := sellStockFromPortfolio(req.UserID, req.AccountID, req.Symbol, req.Quantity, req.Price)
	if portfolioErr != nil {
		fmt.Printf("WARNING: Portfolio update failed: %v\n", portfolioErr)
	}

	// Get user email and send trade completed event
	fmt.Printf("STEP E: Getting user email for notification. UserID=%d, AccountID=%d, RequestEmail=%s\n", req.UserID, req.AccountID, req.Email)
	userEmail := ""
	
	// Priority 1: Use email from request (from JWT token or frontend)
	if req.Email != "" {
		userEmail = req.Email
		fmt.Printf("STEP E COMPLETE: User email retrieved from request (JWT/frontend): UserID=%d, Email=%s\n", req.UserID, userEmail)
	} else {
		// Priority 2: Try to get account info to get email
		acc, accErr := getAccountByUser(req.UserID)
		if accErr == nil && acc.Email != "" {
			userEmail = acc.Email
			fmt.Printf("STEP E COMPLETE: User email retrieved from account (by UserId): UserID=%d, Email=%s\n", req.UserID, userEmail)
		} else {
			fmt.Printf("STEP E: Account email is empty from UserId. Attempting to get by AccountId=%d\n", req.AccountID)
			// Priority 3: Fallback - try to get by AccountId
			accByID, accByIDErr := getAccountByAccountId(req.AccountID)
			if accByIDErr == nil && accByID.Email != "" {
				userEmail = accByID.Email
				fmt.Printf("STEP E COMPLETE: User email retrieved from account (by AccountId): AccountId=%d, Email=%s\n", req.AccountID, userEmail)
			} else {
				accountEmail := ""
				if accErr == nil {
					accountEmail = acc.Email
				}
				accByIDEmail := ""
				if accByIDErr == nil {
					accByIDEmail = accByID.Email
				}
				fmt.Printf("STEP E WARNING: Failed to get user email from all sources. UserID=%d, AccountId=%d, RequestEmail=%s, AccountEmail(from UserId)=%s, AccountEmail(from AccountId)=%s\n", 
					req.UserID, req.AccountID, req.Email, accountEmail, accByIDEmail)
			}
		}
	}

	// Send trade completed event with email
	fmt.Printf("STEP F: Sending trade completed event. Symbol=%s, Type=SELL, Email=%s\n", req.Symbol, userEmail)
	event := &events.TradeCompletedEvent{
		Type:       domain.Sell,
		Symbol:     req.Symbol,
		Price:      req.Price,
		Quantity:   req.Quantity,
		Total:      total,
		AccountID:  req.AccountID,
		ExecutedAt: time.Now(),
		UserEmail:  userEmail,
	}
	eventErr := service.RabbitMQClient.SendTradeCompletedEvent(event)
	if eventErr != nil {
		fmt.Printf("STEP F ERROR: Failed to send trade completed event: %v\n", eventErr)
	} else {
		fmt.Printf("STEP F COMPLETE: Trade completed event sent successfully: Symbol=%s, Type=SELL, Email=%s\n", req.Symbol, userEmail)
	}

	return tradeRes, nil
}

func getAccountByUser(userID int) (accountInfo, error) {
	url := fmt.Sprintf("%s/getAccountByUser/%d", accountServiceBaseURL, userID)
	resp, err := http.Get(url)
	if err != nil {
		return accountInfo{}, err
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		bodyBytes, _ := io.ReadAll(resp.Body)
		fmt.Printf("DEBUG getAccountByUser: Non-OK status for UserID=%d, StatusCode=%d, Body=%s\n", userID, resp.StatusCode, string(bodyBytes))
		return accountInfo{}, fmt.Errorf("account service status code: %d", resp.StatusCode)
	}

	// Read response body to see raw JSON
	bodyBytes, readErr := io.ReadAll(resp.Body)
	if readErr != nil {
		return accountInfo{}, fmt.Errorf("failed to read response body: %w", readErr)
	}
	fmt.Printf("DEBUG getAccountByUser: Raw response for UserID=%d: %s\n", userID, string(bodyBytes))

	// Decode JSON
	var acc accountInfo
	if err := json.Unmarshal(bodyBytes, &acc); err != nil {
		fmt.Printf("DEBUG getAccountByUser: JSON decode error for UserID=%d, Error=%v, Response=%s\n", userID, err, string(bodyBytes))
		return accountInfo{}, err
	}
	fmt.Printf("DEBUG getAccountByUser: Decoded - UserID=%d, AccountId=%d, Email=%s, Balance=%.2f, UserId=%d\n", userID, acc.AccountId, acc.Email, acc.Balance, acc.UserId)
	return acc, nil
}

func getAccountByAccountId(accountID int) (accountInfo, error) {
	url := fmt.Sprintf("%s/getAccountByAccountId/%d", accountServiceBaseURL, accountID)
	fmt.Printf("DEBUG getAccountByAccountId: Calling AccountService URL=%s\n", url)
	resp, err := http.Get(url)
	if err != nil {
		fmt.Printf("DEBUG getAccountByAccountId: HTTP error for AccountId=%d, Error=%v\n", accountID, err)
		return accountInfo{}, err
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		bodyBytes, _ := io.ReadAll(resp.Body)
		fmt.Printf("DEBUG getAccountByAccountId: Non-OK status for AccountId=%d, StatusCode=%d, Body=%s\n", accountID, resp.StatusCode, string(bodyBytes))
		return accountInfo{}, fmt.Errorf("account service status code: %d", resp.StatusCode)
	}

	// Read response body to see raw JSON
	bodyBytes, readErr := io.ReadAll(resp.Body)
	if readErr != nil {
		return accountInfo{}, fmt.Errorf("failed to read response body: %w", readErr)
	}
	fmt.Printf("DEBUG getAccountByAccountId: Raw response for AccountId=%d: %s\n", accountID, string(bodyBytes))

	// Decode JSON
	var acc accountInfo
	if err := json.Unmarshal(bodyBytes, &acc); err != nil {
		fmt.Printf("DEBUG getAccountByAccountId: JSON decode error for AccountId=%d, Error=%v, Response=%s\n", accountID, err, string(bodyBytes))
		return accountInfo{}, err
	}
	fmt.Printf("DEBUG getAccountByAccountId: Decoded - AccountId=%d, Email=%s, Balance=%.2f, UserId=%d\n", accountID, acc.Email, acc.Balance, acc.UserId)
	return acc, nil
}

func getActiveStocksByUser(userID int) ([]portfolioStock, error) {
	url := fmt.Sprintf("%s/getActiveStocksByUser/%d", portfolioServiceBaseURL, userID)
	resp, err := http.Get(url)
	if err != nil {
		return nil, err
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		return nil, fmt.Errorf("portfolio service status code: %d", resp.StatusCode)
	}

	var stocks []portfolioStock
	if err := json.NewDecoder(resp.Body).Decode(&stocks); err != nil {
		return nil, err
	}
	return stocks, nil
}

type updateBalanceRequest struct {
	AccountId int     `json:"accountId"`
	UserId    int     `json:"userId"`
	Amount    float64 `json:"amount"`
}

type updateBalanceResponse struct {
	IsSuccess bool    `json:"isSuccess"`
	Message   string  `json:"message"`
	NewBalance float64 `json:"newBalance"`
}

func updateAccountBalance(accountID, userID int, amount float64) error {
	url := fmt.Sprintf("%s/updateBalance", accountServiceBaseURL)
	
	reqBody := updateBalanceRequest{
		AccountId: accountID,
		UserId:    userID,
		Amount:    amount,
	}

	jsonData, err := json.Marshal(reqBody)
	if err != nil {
		return fmt.Errorf("failed to marshal request: %w", err)
	}

	req, err := http.NewRequest("PUT", url, bytes.NewBuffer(jsonData))
	if err != nil {
		return fmt.Errorf("failed to create request: %w", err)
	}
	req.Header.Set("Content-Type", "application/json")

	client := &http.Client{}
	httpResp, err := client.Do(req)
	if err != nil {
		return fmt.Errorf("failed to send request: %w", err)
	}
	defer httpResp.Body.Close()

	if httpResp.StatusCode != http.StatusOK {
		bodyBytes := make([]byte, 0)
		if httpResp.Body != nil {
			bodyBytes, _ = json.Marshal(httpResp.Body)
		}
		return fmt.Errorf("account service status code: %d, body: %s", httpResp.StatusCode, string(bodyBytes))
	}

	var updateResp updateBalanceResponse
	if err := json.NewDecoder(httpResp.Body).Decode(&updateResp); err != nil {
		return fmt.Errorf("failed to decode response: %w", err)
	}

	if !updateResp.IsSuccess {
		return fmt.Errorf("account balance update failed: %s", updateResp.Message)
	}

	return nil
}

type portfolioInfo struct {
	Id        int `json:"id"`
	UserId    int `json:"userId"`
	AccountId int `json:"accountId"`
}

func getPortfolioByUser(userID int) ([]portfolioInfo, error) {
	url := fmt.Sprintf("%s/getPortfolioByUser?userId=%d", portfolioServiceBaseURL, userID)
	fmt.Printf("DEBUG: Calling Portfolio Service URL: %s\n", url)
	
	resp, err := http.Get(url)
	if err != nil {
		fmt.Printf("ERROR: Failed to call Portfolio Service: %v\n", err)
		return nil, fmt.Errorf("failed to call portfolio service: %w", err)
	}
	defer resp.Body.Close()

	fmt.Printf("DEBUG: Portfolio Service response status: %d\n", resp.StatusCode)

	if resp.StatusCode != http.StatusOK {
		bodyBytes, _ := io.ReadAll(resp.Body)
		return nil, fmt.Errorf("portfolio service status code: %d, body: %s", resp.StatusCode, string(bodyBytes))
	}

	var portfolios []portfolioInfo
	if err := json.NewDecoder(resp.Body).Decode(&portfolios); err != nil {
		return nil, fmt.Errorf("failed to decode portfolio response: %w", err)
	}
	
	fmt.Printf("DEBUG: Found %d portfolio(s) for UserID=%d\n", len(portfolios), userID)
	return portfolios, nil
}

func getPortfolioByAccount(accountID int) (*portfolioInfo, error) {
	url := fmt.Sprintf("%s/getPortfolioByAccount/%d", portfolioServiceBaseURL, accountID)
	fmt.Printf("DEBUG: Calling Portfolio Service URL (by AccountId): %s\n", url)
	
	resp, err := http.Get(url)
	if err != nil {
		fmt.Printf("ERROR: Failed to call Portfolio Service: %v\n", err)
		return nil, fmt.Errorf("failed to call portfolio service: %w", err)
	}
	defer resp.Body.Close()

	fmt.Printf("DEBUG: Portfolio Service response status: %d\n", resp.StatusCode)

	if resp.StatusCode == http.StatusNotFound {
		fmt.Printf("INFO: Portfolio not found for AccountID=%d (404)\n", accountID)
		return nil, nil
	}

	if resp.StatusCode != http.StatusOK {
		bodyBytes, _ := io.ReadAll(resp.Body)
		return nil, fmt.Errorf("portfolio service status code: %d, body: %s", resp.StatusCode, string(bodyBytes))
	}

	var portfolio portfolioInfo
	if err := json.NewDecoder(resp.Body).Decode(&portfolio); err != nil {
		return nil, fmt.Errorf("failed to decode portfolio response: %w", err)
	}
	
	fmt.Printf("DEBUG: Found portfolio ID=%d for AccountID=%d\n", portfolio.Id, accountID)
	return &portfolio, nil
}

type createPortfolioRequest struct {
	UserId    int    `json:"userId"`
	AccountId int    `json:"accountId"`
	Symbol    string `json:"symbol,omitempty"`
	Lot       int    `json:"lot,omitempty"`
}

type createPortfolioResponse struct {
	Success bool   `json:"success"`
	Message string `json:"message"`
	PortfolioId int `json:"portfolioId,omitempty"`
}

func createPortfolio(userID, accountID int) (int, error) {
	url := fmt.Sprintf("%s/createPortfolio", portfolioServiceBaseURL)
	fmt.Printf("DEBUG: Calling Portfolio Service URL: %s\n", url)
	fmt.Printf("DEBUG: Request body: UserId=%d, AccountId=%d\n", userID, accountID)
	
	reqBody := createPortfolioRequest{
		UserId:    userID,
		AccountId: accountID,
	}

	jsonData, err := json.Marshal(reqBody)
	if err != nil {
		return 0, fmt.Errorf("failed to marshal request: %w", err)
	}

	req, err := http.NewRequest("POST", url, bytes.NewBuffer(jsonData))
	if err != nil {
		return 0, fmt.Errorf("failed to create request: %w", err)
	}
	req.Header.Set("Content-Type", "application/json")
	req.Header.Set("Accept", "application/json")

	fmt.Printf("DEBUG: Request JSON: %s\n", string(jsonData))

	client := &http.Client{
		Timeout: 30 * time.Second,
	}
	
	fmt.Printf("DEBUG: Sending HTTP POST request to: %s\n", url)
	httpResp, err := client.Do(req)
	if err != nil {
		fmt.Printf("ERROR: HTTP request failed: %v\n", err)
		return 0, fmt.Errorf("failed to send request to Portfolio Service: %w", err)
	}
	defer httpResp.Body.Close()

	bodyBytes, readErr := io.ReadAll(httpResp.Body)
	if readErr != nil {
		fmt.Printf("WARNING: Failed to read response body: %v\n", readErr)
	}

	fmt.Printf("DEBUG: Portfolio Service response status: %d\n", httpResp.StatusCode)
	fmt.Printf("DEBUG: Portfolio Service response body: %s\n", string(bodyBytes))

	if httpResp.StatusCode != http.StatusOK {
		return 0, fmt.Errorf("portfolio service status code (createPortfolio): %d, URL: %s, body: %s", httpResp.StatusCode, url, string(bodyBytes))
	}

	var createResp createPortfolioResponse
	if err := json.Unmarshal(bodyBytes, &createResp); err != nil {
		return 0, fmt.Errorf("failed to decode response: %w, body: %s", err, string(bodyBytes))
	}

	if !createResp.Success {
		return 0, fmt.Errorf("portfolio creation failed: %s", createResp.Message)
	}

	portfolios, err := getPortfolioByUser(userID)
	if err != nil {
		return 0, fmt.Errorf("failed to get created portfolio: %w", err)
	}

	for _, p := range portfolios {
		if p.AccountId == accountID {
			return p.Id, nil
		}
	}

	if len(portfolios) > 0 {
		return portfolios[0].Id, nil
	}

	return 0, fmt.Errorf("portfolio created but ID could not be retrieved")
}

type addStockRequest struct {
	PortfolioId  int     `json:"portfolioId"`
	Symbol       string  `json:"symbol"`
	Lot          float64 `json:"lot"`
	PricePerShare float64 `json:"pricePerShare"`
}

type addStockResponse struct {
	Success bool   `json:"success"`
	Message string `json:"message"`
}

func addStockToPortfolio(userID, accountID int, symbol string, lot, pricePerShare float64) error {
	fmt.Printf("STEP D.1: Looking for portfolio with AccountID=%d\n", accountID)
	
	portfolio, err := getPortfolioByAccount(accountID)
	if err != nil {
		return fmt.Errorf("failed to get portfolio by AccountId: %w", err)
	}

	var portfolioID int
	if portfolio == nil {
		fmt.Printf("STEP D.2: No portfolio found for AccountID=%d, UserID=%d, creating new portfolio...\n", accountID, userID)
		createdPortfolioID, createErr := createPortfolio(userID, accountID)
		if createErr != nil {
			return fmt.Errorf("failed to create portfolio: %w", createErr)
		}
		portfolioID = createdPortfolioID
		fmt.Printf("STEP D.2 COMPLETE: Portfolio created with ID=%d for UserID=%d, AccountID=%d\n", portfolioID, userID, accountID)
	} else {
		portfolioID = portfolio.Id
		fmt.Printf("STEP D.1 COMPLETE: Found existing portfolio ID=%d for AccountID=%d\n", portfolioID, accountID)
	}

	fmt.Printf("STEP D.3: Adding stock to portfolio. PortfolioID=%d, Symbol=%s, Lot=%.2f, PricePerShare=%.2f\n", 
		portfolioID, symbol, lot, pricePerShare)

	url := fmt.Sprintf("%s/addStock", portfolioServiceBaseURL)
	fmt.Printf("DEBUG: Calling Portfolio Service URL: %s\n", url)
	fmt.Printf("DEBUG: Request body: PortfolioId=%d, Symbol=%s, Lot=%.2f, PricePerShare=%.2f\n", 
		portfolioID, symbol, lot, pricePerShare)
	
	reqBody := addStockRequest{
		PortfolioId:  portfolioID,
		Symbol:       symbol,
		Lot:          lot,
		PricePerShare: pricePerShare,
	}

	jsonData, err := json.Marshal(reqBody)
	if err != nil {
		return fmt.Errorf("failed to marshal request: %w", err)
	}

	req, err := http.NewRequest("POST", url, bytes.NewBuffer(jsonData))
	if err != nil {
		return fmt.Errorf("failed to create request: %w", err)
	}
	req.Header.Set("Content-Type", "application/json")

	client := &http.Client{}
	httpResp, err := client.Do(req)
	if err != nil {
		fmt.Printf("ERROR: HTTP request failed: %v\n", err)
		return fmt.Errorf("failed to send request: %w", err)
	}
	defer httpResp.Body.Close()

	fmt.Printf("DEBUG: Portfolio Service response status: %d\n", httpResp.StatusCode)

	if httpResp.StatusCode != http.StatusOK {
		bodyBytes, _ := io.ReadAll(httpResp.Body)
		fmt.Printf("ERROR: Portfolio Service error response: %s\n", string(bodyBytes))
		return fmt.Errorf("portfolio service status code: %d, body: %s", httpResp.StatusCode, string(bodyBytes))
	}

	var addResp addStockResponse
	if err := json.NewDecoder(httpResp.Body).Decode(&addResp); err != nil {
		return fmt.Errorf("failed to decode response: %w", err)
	}

	if !addResp.Success {
		return fmt.Errorf("portfolio add stock failed: %s", addResp.Message)
	}

	fmt.Printf("STEP D.3 COMPLETE: Stock added successfully to portfolio ID=%d\n", portfolioID)
	return nil
}

type sellStockRequest struct {
	PortfolioId  int     `json:"portfolioId"`
	Symbol       string  `json:"symbol"`
	Lot          float64 `json:"lot"`
	PricePerShare float64 `json:"pricePerShare"`
}

type sellStockResponse struct {
	Success bool   `json:"success"`
	Message string `json:"message"`
}

func sellStockFromPortfolio(userID, accountID int, symbol string, lot, pricePerShare float64) error {
	portfolios, err := getPortfolioByUser(userID)
	if err != nil {
		return fmt.Errorf("failed to get portfolio: %w", err)
	}

	if len(portfolios) == 0 {
		return fmt.Errorf("no portfolio found for user %d", userID)
	}

	var portfolioID int
	for _, p := range portfolios {
		if p.AccountId == accountID {
			portfolioID = p.Id
			break
		}
	}
	if portfolioID == 0 {
		portfolioID = portfolios[0].Id
	}

	url := fmt.Sprintf("%s/sellStock", portfolioServiceBaseURL)
	
	reqBody := sellStockRequest{
		PortfolioId:  portfolioID,
		Symbol:       symbol,
		Lot:          lot,
		PricePerShare: pricePerShare,
	}

	jsonData, err := json.Marshal(reqBody)
	if err != nil {
		return fmt.Errorf("failed to marshal request: %w", err)
	}

	req, err := http.NewRequest("POST", url, bytes.NewBuffer(jsonData))
	if err != nil {
		return fmt.Errorf("failed to create request: %w", err)
	}
	req.Header.Set("Content-Type", "application/json")

	client := &http.Client{}
	httpResp, err := client.Do(req)
	if err != nil {
		return fmt.Errorf("failed to send request: %w", err)
	}
	defer httpResp.Body.Close()

	if httpResp.StatusCode != http.StatusOK {
		bodyBytes, _ := io.ReadAll(httpResp.Body)
		return fmt.Errorf("portfolio service status code: %d, body: %s", httpResp.StatusCode, string(bodyBytes))
	}

	var sellResp sellStockResponse
	if err := json.NewDecoder(httpResp.Body).Decode(&sellResp); err != nil {
		return fmt.Errorf("failed to decode response: %w", err)
	}

	if !sellResp.Success {
		return fmt.Errorf("portfolio sell stock failed: %s", sellResp.Message)
	}

	return nil
}

func (s *GrpcTradeServer) CreateTrade(ctx context.Context, req *pb.CreateTradeRequest) (*pb.CreateTradeResponse, error) {
	fmt.Printf("STEP 1: gRPC CreateTrade çağrıldı - AccountId=%d, Symbol=%s, Quantity=%.2f, Price=%.2f, Type=%d\n",
		req.AccountId, req.Symbol, req.Quantity, req.Price, req.Type)

	tradeRequest := request.CreateTradeRequest{
		AccountID: int(req.AccountId),
		Symbol:    req.Symbol,
		Type:      domain.TradeType(req.Type),
		Quantity:  float64(req.Quantity),
		Price:     float64(req.Price),
		Total:     float64(req.Price) * float64(req.Quantity),
	}

	fmt.Printf("STEP 2: TradeService.CreateTrade çağrılıyor - AccountID=%d, Symbol=%s, Quantity=%.2f, Price=%.2f\n",
		tradeRequest.AccountID, tradeRequest.Symbol, tradeRequest.Quantity, tradeRequest.Price)

	tradeResponse, err := s.TradeService.CreateTrade(ctx, tradeRequest)
	if err != nil {
		fmt.Printf("STEP 3 ERROR: TradeService.CreateTrade hatası: %v\n", err)
		return nil, err
	}

	fmt.Printf("STEP 3 SUCCESS: TradeService.CreateTrade başarılı - TradeId=%d, Message=%s\n",
		tradeResponse.TradeId, tradeResponse.Message)

	return &pb.CreateTradeResponse{
		Message: tradeResponse.Message,
		TradeId: tradeResponse.TradeId,
	}, nil
}

func getUserEmailByUserID(userID int) (string, error) {
	url := fmt.Sprintf("%s/%d", authUserServiceBaseURL, userID)
	fmt.Printf("DEBUG: Calling AuthUserService to get email: URL=%s, UserID=%d\n", url, userID)
	resp, err := http.Get(url)
	if err != nil {
		fmt.Printf("ERROR: Failed to call AuthUserService: URL=%s, Error=%v\n", url, err)
		return "", err
	}
	defer resp.Body.Close()

	fmt.Printf("DEBUG: AuthUserService response: StatusCode=%d, UserID=%d\n", resp.StatusCode, userID)

	if resp.StatusCode != http.StatusOK {
		bodyBytes, _ := io.ReadAll(resp.Body)
		fmt.Printf("ERROR: AuthUserService returned non-OK status: StatusCode=%d, Body=%s, UserID=%d\n", resp.StatusCode, string(bodyBytes), userID)
		return "", fmt.Errorf("auth user service status code: %d", resp.StatusCode)
	}

	var user userInfo
	if err := json.NewDecoder(resp.Body).Decode(&user); err != nil {
		fmt.Printf("ERROR: Failed to decode AuthUserService response: Error=%v, UserID=%d\n", err, userID)
		return "", err
	}
	
	fmt.Printf("DEBUG: User info decoded: UserID=%d, Email=%s\n", userID, user.Email)
	return user.Email, nil
}

func (t TradeService) CreateTrade(ctx context.Context, request request.CreateTradeRequest) (res response.CreateTradeResponse, err error) {

	trade := &domain.Trade{
		Price:      request.Price,
		Quantity:   request.Quantity,
		Symbol:     request.Symbol,
		Status:     "",
		Total:      request.Total,
		ExecutedAt: time.Now(),
		AccountID:  request.AccountID,
		Type:       request.Type,
	}
	err = t.tradeRepository.Create(ctx, trade)
	if err != nil {
		return response.CreateTradeResponse{}, err
	}

	// GORM Create işleminden sonra ID otomatik olarak set edilir
	res.TradeId = int32(trade.ID)
	res.Message = "Trade created successfully"

	// Note: Trade completed event is now sent from DirectBuy/DirectSell functions
	// to ensure we have user email before sending the event

	return res, nil
}

func (t TradeService) GetTradeByAccount(ctx context.Context, tradeId int) (tradeResponse response.GetTradeResponse, err error) {
	trade, err := t.tradeRepository.GetByAccount(ctx, tradeId)
	if err != nil {
		return tradeResponse, err
	}

	tradeResponse.Status = trade.Status
	tradeResponse.Total = trade.Total
	tradeResponse.Symbol = trade.Symbol
	tradeResponse.Price = trade.Price
	tradeResponse.Quantity = trade.Quantity
	tradeResponse.ExecutedAt = trade.ExecutedAt
	tradeResponse.Type = trade.Type

	return tradeResponse, nil

}

func (t TradeService) GetAllTrade(ctx context.Context) (responses []response.GetTradeResponse, err error) {

	trades, err := t.tradeRepository.GetAll(ctx)
	if err != nil {
		return nil, err
	}

	responses = make([]response.GetTradeResponse, len(trades))

	for i, trade := range trades {
		responses[i] = response.GetTradeResponse{
			Status:     trade.Status,
			Total:      trade.Total,
			Symbol:     trade.Symbol,
			Price:      trade.Price,
			Quantity:   trade.Quantity,
			ExecutedAt: trade.ExecutedAt,
			Type:       trade.Type,
		}
	}

	return responses, nil
}

func (t TradeService) GetTradeHistoryByAccountId(ctx context.Context, accountId int) (responses []response.GetTradeResponse, err error) {
	trades, err := t.tradeRepository.GetByAccountId(ctx, accountId)
	if err != nil {
		return nil, err
	}

	responses = make([]response.GetTradeResponse, len(trades))

	for i, trade := range trades {
		responses[i] = response.GetTradeResponse{
			Status:     trade.Status,
			Total:      trade.Total,
			Symbol:     trade.Symbol,
			Price:      trade.Price,
			Quantity:   trade.Quantity,
			ExecutedAt: trade.ExecutedAt,
			Type:       trade.Type,
		}
	}

	return responses, nil
}
