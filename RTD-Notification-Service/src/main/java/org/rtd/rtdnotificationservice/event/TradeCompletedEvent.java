package org.rtd.rtdnotificationservice.event;

import com.fasterxml.jackson.annotation.JsonCreator;
import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import com.fasterxml.jackson.annotation.JsonProperty;
import org.rtd.rtdnotificationservice.enums.TradeType;

import java.math.BigDecimal;
import java.time.OffsetDateTime;

@JsonIgnoreProperties(ignoreUnknown = true)
public class TradeCompletedEvent {
    @JsonProperty("account_id")
    private int accountId;
    
    @JsonProperty("symbol")
    private String symbol;
    
    @JsonProperty("trade_type")
    private TradeType tradeType;
    
    @JsonProperty("quantity")
    private BigDecimal quantity;
    
    @JsonProperty("price")
    private BigDecimal price;
    
    @JsonProperty("total")
    private BigDecimal total;
    
    @JsonProperty("executed_at")
    private OffsetDateTime executedAt;
    
    @JsonProperty("user_email")
    private String userEmail;

    public TradeCompletedEvent() {
    }

    @JsonCreator
    public TradeCompletedEvent(
            @JsonProperty("account_id") int accountId,
            @JsonProperty("symbol") String symbol,
            @JsonProperty("trade_type") TradeType tradeType,
            @JsonProperty("quantity") BigDecimal quantity,
            @JsonProperty("price") BigDecimal price,
            @JsonProperty("total") BigDecimal total,
            @JsonProperty("executed_at") OffsetDateTime executedAt,
            @JsonProperty("user_email") String userEmail) {
        this.accountId = accountId;
        this.symbol = symbol;
        this.tradeType = tradeType;
        this.quantity = quantity;
        this.price = price;
        this.total = total;
        this.executedAt = executedAt;
        this.userEmail = userEmail;
    }

    @JsonProperty("account_id")
    public int getAccountId() {
        return accountId;
    }

    @JsonProperty("account_id")
    public void setAccountId(int accountId) {
        this.accountId = accountId;
    }

    public String getSymbol() {
        return symbol;
    }

    public void setSymbol(String symbol) {
        this.symbol = symbol;
    }

    @JsonProperty("trade_type")
    public TradeType getTradeType() {
        return tradeType;
    }

    @JsonProperty("trade_type")
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

    @JsonProperty("executed_at")
    public OffsetDateTime getExecutedAt() {
        return executedAt;
    }

    @JsonProperty("executed_at")
    public void setExecutedAt(OffsetDateTime executedAt) {
        this.executedAt = executedAt;
    }

    @JsonProperty("user_email")
    public String getUserEmail() {
        return userEmail;
    }

    @JsonProperty("user_email")
    public void setUserEmail(String userEmail) {
        this.userEmail = userEmail;
    }
}
