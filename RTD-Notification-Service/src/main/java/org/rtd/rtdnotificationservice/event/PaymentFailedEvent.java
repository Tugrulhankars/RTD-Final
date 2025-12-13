package org.rtd.rtdnotificationservice.event;

import java.math.BigDecimal;
import java.time.LocalDateTime;

public class PaymentFailedEvent {
    private int userId;
    private BigDecimal amount;
    private String currency = "TRY";
    private String paymentTransactionId;
    private String paymentMethod;
    private LocalDateTime paymentDate;
    private String email;
    private String status = "FAILED";
    private String failureReason;
    private String errorCode;
    private String errorMessage;

    public PaymentFailedEvent() {
    }

    public PaymentFailedEvent(int userId, BigDecimal amount, String currency, String paymentTransactionId,
                             String paymentMethod, LocalDateTime paymentDate, String email, String status,
                             String failureReason, String errorCode, String errorMessage) {
        this.userId = userId;
        this.amount = amount;
        this.currency = currency != null ? currency : "TRY";
        this.paymentTransactionId = paymentTransactionId;
        this.paymentMethod = paymentMethod;
        this.paymentDate = paymentDate;
        this.email = email;
        this.status = status != null ? status : "FAILED";
        this.failureReason = failureReason;
        this.errorCode = errorCode;
        this.errorMessage = errorMessage;
    }

    public int getUserId() {
        return userId;
    }

    public void setUserId(int userId) {
        this.userId = userId;
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
        this.status = status != null ? status : "FAILED";
    }

    public String getFailureReason() {
        return failureReason;
    }

    public void setFailureReason(String failureReason) {
        this.failureReason = failureReason;
    }

    public String getErrorCode() {
        return errorCode;
    }

    public void setErrorCode(String errorCode) {
        this.errorCode = errorCode;
    }

    public String getErrorMessage() {
        return errorMessage;
    }

    public void setErrorMessage(String errorMessage) {
        this.errorMessage = errorMessage;
    }
}

