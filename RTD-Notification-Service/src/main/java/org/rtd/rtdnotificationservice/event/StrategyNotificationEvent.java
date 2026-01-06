package org.rtd.rtdnotificationservice.event;

import com.fasterxml.jackson.annotation.JsonProperty;
import com.fasterxml.jackson.databind.annotation.JsonDeserialize;
import com.fasterxml.jackson.datatype.jsr310.deser.LocalDateTimeDeserializer;
import java.math.BigDecimal;
import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.List;

public class StrategyNotificationEvent {
    @JsonProperty("strategyId")
    private int strategyId;
    
    @JsonProperty("userId")
    private int userId;
    
    @JsonProperty("userEmail")
    private String userEmail;
    
    @JsonProperty("strategyName")
    private String strategyName;
    
    @JsonProperty("stockSymbol")
    private String stockSymbol;
    
    @JsonProperty("status")
    private String status;
    
    @JsonProperty("action")
    private String action;
    
    @JsonProperty("buyPrice")
    private BigDecimal buyPrice;
    
    @JsonProperty("sellPrice")
    private BigDecimal sellPrice;
    
    @JsonProperty("profitLoss")
    private BigDecimal profitLoss;
    
    @JsonProperty("currentPrice")
    private BigDecimal currentPrice;
    
    @JsonProperty("timestamp")
    @JsonDeserialize(using = LocalDateTimeDeserializer.class)
    private LocalDateTime timestamp;
    
    @JsonProperty("executedRules")
    private List<RuleExecutionInfo> executedRules = new ArrayList<>();
    
    @JsonProperty("reason")
    private String reason;

    public StrategyNotificationEvent() {
    }

    public StrategyNotificationEvent(int strategyId, int userId, String userEmail, String strategyName, String stockSymbol, 
                                     String status, String action, BigDecimal buyPrice, BigDecimal sellPrice, 
                                     BigDecimal profitLoss, BigDecimal currentPrice, LocalDateTime timestamp, 
                                     List<RuleExecutionInfo> executedRules, String reason) {
        this.strategyId = strategyId;
        this.userId = userId;
        this.userEmail = userEmail;
        this.strategyName = strategyName;
        this.stockSymbol = stockSymbol;
        this.status = status;
        this.action = action;
        this.buyPrice = buyPrice;
        this.sellPrice = sellPrice;
        this.profitLoss = profitLoss;
        this.currentPrice = currentPrice;
        this.timestamp = timestamp;
        this.executedRules = executedRules != null ? executedRules : new ArrayList<>();
        this.reason = reason;
    }

    public int getStrategyId() {
        return strategyId;
    }

    public void setStrategyId(int strategyId) {
        this.strategyId = strategyId;
    }

    public int getUserId() {
        return userId;
    }

    public void setUserId(int userId) {
        this.userId = userId;
    }

    public String getUserEmail() {
        return userEmail;
    }

    public void setUserEmail(String userEmail) {
        this.userEmail = userEmail;
    }

    public String getStrategyName() {
        return strategyName;
    }

    public void setStrategyName(String strategyName) {
        this.strategyName = strategyName;
    }

    public String getStockSymbol() {
        return stockSymbol;
    }

    public void setStockSymbol(String stockSymbol) {
        this.stockSymbol = stockSymbol;
    }

    public String getStatus() {
        return status;
    }

    public void setStatus(String status) {
        this.status = status;
    }

    public String getAction() {
        return action;
    }

    public void setAction(String action) {
        this.action = action;
    }

    public BigDecimal getBuyPrice() {
        return buyPrice;
    }

    public void setBuyPrice(BigDecimal buyPrice) {
        this.buyPrice = buyPrice;
    }

    public BigDecimal getSellPrice() {
        return sellPrice;
    }

    public void setSellPrice(BigDecimal sellPrice) {
        this.sellPrice = sellPrice;
    }

    public BigDecimal getProfitLoss() {
        return profitLoss;
    }

    public void setProfitLoss(BigDecimal profitLoss) {
        this.profitLoss = profitLoss;
    }

    public BigDecimal getCurrentPrice() {
        return currentPrice;
    }

    public void setCurrentPrice(BigDecimal currentPrice) {
        this.currentPrice = currentPrice;
    }

    public LocalDateTime getTimestamp() {
        return timestamp;
    }

    public void setTimestamp(LocalDateTime timestamp) {
        this.timestamp = timestamp;
    }

    public List<RuleExecutionInfo> getExecutedRules() {
        return executedRules;
    }

    public void setExecutedRules(List<RuleExecutionInfo> executedRules) {
        this.executedRules = executedRules != null ? executedRules : new ArrayList<>();
    }

    public String getReason() {
        return reason;
    }

    public void setReason(String reason) {
        this.reason = reason;
    }
}
