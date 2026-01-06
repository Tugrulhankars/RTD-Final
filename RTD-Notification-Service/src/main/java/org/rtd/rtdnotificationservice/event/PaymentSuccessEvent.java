package org.rtd.rtdnotificationservice.event;

import com.fasterxml.jackson.annotation.JsonProperty;
import java.math.BigDecimal;
import java.time.LocalDateTime;

public class PaymentSuccessEvent {
    @JsonProperty("userId")
    private int userId;
    
    @JsonProperty("accountId")
    private int accountId;
    
    @JsonProperty("amount")
    private BigDecimal amount;
    
    @JsonProperty("currency")
    private String currency = "TRY";
    
    @JsonProperty("paymentTransactionId")
    private String paymentTransactionId;
    
    @JsonProperty("paymentMethod")
    private String paymentMethod;
    
    @JsonProperty("paymentDate")
    private LocalDateTime paymentDate;
    
    @JsonProperty("email")
    private String email;
    
    @JsonProperty("status")
    private String status = "SUCCESS";
    
    @JsonProperty("message")
    private String message;

    public PaymentSuccessEvent() {
    }

    public PaymentSuccessEvent(int userId, int accountId, BigDecimal amount, String currency,
                              String paymentTransactionId, String paymentMethod, LocalDateTime paymentDate,
                              String email, String status, String message) {
        this.userId = userId;
        this.accountId = accountId;
        this.amount = amount;
        this.currency = currency != null ? currency : "TRY";
        this.paymentTransactionId = paymentTransactionId;
        this.paymentMethod = paymentMethod;
        this.paymentDate = paymentDate;
        this.email = email;
        this.status = status != null ? status : "SUCCESS";
        this.message = message;
    }

    public int getUserId() {
        return userId;
    }

    public void setUserId(int userId) {
        this.userId = userId;
    }

    public int getAccountId() {
        return accountId;
    }

    public void setAccountId(int accountId) {
        this.accountId = accountId;
    }

    public BigDecimal getAmount() {
        return amount;
    }

    public void setAmount(BigDecimal amount) {
        this.amount = amount;
    }

    public String getCurrency() {
        return currency;
    }

    public void setCurrency(String currency) {
        this.currency = currency != null ? currency : "TRY";
    }

    public String getPaymentTransactionId() {
        return paymentTransactionId;
    }

    public void setPaymentTransactionId(String paymentTransactionId) {
        this.paymentTransactionId = paymentTransactionId;
    }

    public String getPaymentMethod() {
        return paymentMethod;
    }

    public void setPaymentMethod(String paymentMethod) {
        this.paymentMethod = paymentMethod;
    }

    public LocalDateTime getPaymentDate() {
        return paymentDate;
    }

    public void setPaymentDate(LocalDateTime paymentDate) {
        this.paymentDate = paymentDate;
    }

    public String getEmail() {
        return email;
    }

    public void setEmail(String email) {
        this.email = email;
    }

    public String getStatus() {
        return status;
    }

    public void setStatus(String status) {
        this.status = status != null ? status : "SUCCESS";
    }

    public String getMessage() {
        return message;
    }

    public void setMessage(String message) {
        this.message = message;
    }
}
