package org.rtd.rtdnotificationservice.event;

import org.rtd.rtdnotificationservice.enums.TradeType;

import java.math.BigDecimal;
import java.time.LocalDateTime;

public class TradeCompletedEvent {
    private int accountId;
    private String symbol;
    private TradeType tradeType;
    private BigDecimal quantity;
    private BigDecimal price;
    private BigDecimal total;
    private LocalDateTime executedAt;
    private String userEmail;

    public TradeCompletedEvent() {
    }

    public TradeCompletedEvent(int accountId, String symbol, TradeType tradeType, BigDecimal quantity, 
                              BigDecimal price, BigDecimal total, LocalDateTime executedAt, String userEmail) {
        this.accountId = accountId;
        this.symbol = symbol;
        this.tradeType = tradeType;
        this.quantity = quantity;
        this.price = price;
        this.total = total;
        this.executedAt = executedAt;
        this.userEmail = userEmail;
    }

    public int getAccountId() {
        return accountId;
    }

    public void setAccountId(int accountId) {
        this.accountId = accountId;
    }

    public String getSymbol() {
        return symbol;
    }

    public void setSymbol(String symbol) {
        this.symbol = symbol;
    }

    public TradeType getTradeType() {
        return tradeType;
    }

    public void setTradeType(TradeType tradeType) {
        this.tradeType = tradeType;
    }

    public BigDecimal getQuantity() {
        return quantity;
    }

    public void setQuantity(BigDecimal quantity) {
        this.quantity = quantity;
    }

    public BigDecimal getPrice() {
        return price;
    }

    public void setPrice(BigDecimal price) {
        this.price = price;
    }

    public BigDecimal getTotal() {
        return total;
    }

    public void setTotal(BigDecimal total) {
        this.total = total;
    }

    public LocalDateTime getExecutedAt() {
        return executedAt;
    }

    public void setExecutedAt(LocalDateTime executedAt) {
        this.executedAt = executedAt;
    }

    public String getUserEmail() {
        return userEmail;
    }

    public void setUserEmail(String userEmail) {
        this.userEmail = userEmail;
    }
}
