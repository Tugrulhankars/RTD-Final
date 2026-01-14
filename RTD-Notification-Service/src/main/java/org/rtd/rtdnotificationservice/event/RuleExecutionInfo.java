package org.rtd.rtdnotificationservice.event;

import com.fasterxml.jackson.annotation.JsonProperty;
import java.math.BigDecimal;
import java.time.OffsetDateTime;

public class RuleExecutionInfo {
    @JsonProperty("ruleName")
    private String ruleName;
    
    @JsonProperty("step")
    private int step;
    
    @JsonProperty("action")
    private String action;
    
    @JsonProperty("reason")
    private String reason;
    
    @JsonProperty("price")
    private BigDecimal price;
    
    @JsonProperty("timestamp")
    private OffsetDateTime timestamp;

    public RuleExecutionInfo() {
    }

    public RuleExecutionInfo(String ruleName, int step, String action, String reason, BigDecimal price, OffsetDateTime timestamp) {
        this.ruleName = ruleName;
        this.step = step;
        this.action = action;
        this.reason = reason;
        this.price = price;
        this.timestamp = timestamp;
    }

    public String getRuleName() {
        return ruleName;
    }

    public void setRuleName(String ruleName) {
        this.ruleName = ruleName;
    }

    public int getStep() {
        return step;
    }

    public void setStep(int step) {
        this.step = step;
    }

    public String getAction() {
        return action;
    }

    public void setAction(String action) {
        this.action = action;
    }

    public String getReason() {
        return reason;
    }

    public void setReason(String reason) {
        this.reason = reason;
    }

    public BigDecimal getPrice() {
        return price;
    }

    public void setPrice(BigDecimal price) {
        this.price = price;
    }

    public OffsetDateTime getTimestamp() {
        return timestamp;
    }

    public void setTimestamp(OffsetDateTime timestamp) {
        this.timestamp = timestamp;
    }
}
