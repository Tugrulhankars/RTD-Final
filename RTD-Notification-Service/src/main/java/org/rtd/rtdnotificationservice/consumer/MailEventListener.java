package org.rtd.rtdnotificationservice.consumer;

import com.fasterxml.jackson.core.JsonProcessingException;
import jakarta.mail.MessagingException;
import org.rtd.rtdnotificationservice.constants.RabbitMQConstants;
import org.rtd.rtdnotificationservice.event.OtpCreatedEvent;
import org.rtd.rtdnotificationservice.event.OtpCreatedPasswordResetEvent;
import org.rtd.rtdnotificationservice.event.OtpVerifiedEvent;
import org.rtd.rtdnotificationservice.event.PaymentFailedEvent;
import org.rtd.rtdnotificationservice.event.PaymentSuccessEvent;
import org.rtd.rtdnotificationservice.event.StrategyNotificationEvent;
import org.rtd.rtdnotificationservice.event.TradeCompletedEvent;
import org.rtd.rtdnotificationservice.event.UserRegisteredEvent;
import org.rtd.rtdnotificationservice.service.MailService;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.messaging.handler.annotation.Payload;
import org.springframework.stereotype.Service;

@Service
public class MailEventListener {
    private static final Logger log = LoggerFactory.getLogger(MailEventListener.class);
    private final MailService mailService;
    public MailEventListener(MailService mailService) {
        this.mailService = mailService;
    }
    @RabbitListener(queues = RabbitMQConstants.notificationEmailOtpQueue, containerFactory = "rabbitListenerContainerFactory")
    public void handleOtpCreatedEvent(@Payload OtpCreatedEvent event) throws MessagingException, JsonProcessingException {
        try {
            if (event == null) {
                log.error("OtpCreatedEvent is null - skipping email notification");
                return;
            }
            
            if (event.getEmail() == null || event.getEmail().trim().isEmpty()) {
                log.error("OtpCreatedEvent email is null or empty. Email notification cannot be sent. Please ensure email is properly set in the event.");
                return;
            }
            
            log.info("OTP Mail Event received - Email: {}, OTP Code: {}", event.getEmail(), event.getVerifyOtpCode());
            mailService.sendOtpMail(event);
            log.info("OTP Mail sent successfully to: {}", event.getEmail());
        }catch (MessagingException e) {
            log.error("Error sending OTP mail to: {} - Error: {}", event != null ? event.getEmail() : "unknown", e.getMessage(), e);
            throw e;
        } catch (Exception e) {
            log.error("Unexpected error in handleOtpCreatedEvent for email: {} - Error: {}", 
                    event != null ? event.getEmail() : "unknown", e.getMessage(), e);
            throw new MessagingException("Failed to send OTP mail", e);
        }
    }

    @RabbitListener(queues = RabbitMQConstants.notificationEmailUserRegisterQueue, containerFactory = "rabbitListenerContainerFactory")
    public void handleUserRegisteredEvent(@Payload UserRegisteredEvent event) throws MessagingException, JsonProcessingException {
        try {
            if (event == null) {
                log.error("UserRegisteredEvent is null - skipping email notification");
                return;
            }
            
            if (event.getEmail() == null || event.getEmail().trim().isEmpty()) {
                log.error("UserRegisteredEvent email is null or empty. Email notification cannot be sent. Please ensure email is properly set in the event.");
                return;
            }
            
            mailService.sendWelcomeMail(event);
            log.info("Welcome mail sent successfully to: {}", event.getEmail());
        }catch (MessagingException e) {
            log.error("Error sending welcome mail to: {} - Error: {}", 
                    event != null ? event.getEmail() : "unknown", e.getMessage(), e);
            throw  e;
        } catch (Exception e) {
            log.error("Unexpected error in handleUserRegisteredEvent for email: {} - Error: {}", 
                    event != null ? event.getEmail() : "unknown", e.getMessage(), e);
            throw new MessagingException("Failed to send welcome mail", e);
        }
    }

    @RabbitListener(queues = RabbitMQConstants.notificationEmailUserIsVerifyQueue, containerFactory = "rabbitListenerContainerFactory")
    public void handleOtpVerifiedEvent(@Payload OtpVerifiedEvent event) throws MessagingException {
        try {
            if (event == null) {
                log.error("OtpVerifiedEvent is null - skipping email notification");
                return;
            }
            
            if (event.getEmail() == null || event.getEmail().trim().isEmpty()) {
                log.error("OtpVerifiedEvent email is null or empty. Email notification cannot be sent. Please ensure email is properly set in the event.");
                return;
            }
            
            mailService.sendUserVerifiedMail(event);
            log.info("User verified mail sent successfully to: {}", event.getEmail());
        }catch (MessagingException e) {
            log.error("Error sending user verified mail to: {} - Error: {}", 
                    event != null ? event.getEmail() : "unknown", e.getMessage(), e);
            throw  e;
        } catch (Exception e) {
            log.error("Unexpected error in handleOtpVerifiedEvent for email: {} - Error: {}", 
                    event != null ? event.getEmail() : "unknown", e.getMessage(), e);
            throw new MessagingException("Failed to send user verified mail", e);
        }
    }

    @RabbitListener(queues = RabbitMQConstants.notificationPasswordResetOtpQueue, containerFactory = "rabbitListenerContainerFactory")
    public void handlePasswordResetOtpEvent(@Payload OtpCreatedPasswordResetEvent event) throws MessagingException {
        try {
            if (event == null) {
                log.error("OtpCreatedPasswordResetEvent is null - skipping email notification");
                return;
            }
            
            if (event.getEmail() == null || event.getEmail().trim().isEmpty()) {
                log.error("OtpCreatedPasswordResetEvent email is null or empty. Email notification cannot be sent. Please ensure email is properly set in the event.");
                return;
            }
            
            mailService.sendPasswordResetOtpMail(event);
            log.info("Password reset OTP mail sent successfully to: {}", event.getEmail());
        } catch (MessagingException e) {
            log.error("Error sending password reset OTP mail to: {} - Error: {}", 
                    event != null ? event.getEmail() : "unknown", e.getMessage(), e);
            throw e;
        } catch (Exception e) {
            log.error("Unexpected error in handlePasswordResetOtpEvent for email: {} - Error: {}", 
                    event != null ? event.getEmail() : "unknown", e.getMessage(), e);
            throw new MessagingException("Failed to send password reset OTP mail", e);
        }
    }

    @RabbitListener(queues = RabbitMQConstants.notificationStrategyNotificationQueue, containerFactory = "rabbitListenerContainerFactory")
    public void handleStrategyNotificationEvent(@Payload StrategyNotificationEvent event) throws MessagingException {
        try {
            if (event == null) {
                log.error("StrategyNotificationEvent is null - skipping email notification");
                return;
            }
            
            log.info("StrategyNotificationEvent received - Raw event data: StrategyId={}, UserId={}, UserEmail={}, StrategyName={}, StockSymbol={}, Status={}, Action={}, CurrentPrice={}, Timestamp={}", 
                    event.getStrategyId(), 
                    event.getUserId(), 
                    event.getUserEmail() != null ? event.getUserEmail() : "null",
                    event.getStrategyName() != null ? event.getStrategyName() : "null",
                    event.getStockSymbol() != null ? event.getStockSymbol() : "null",
                    event.getStatus() != null ? event.getStatus() : "null",
                    event.getAction() != null ? event.getAction() : "null",
                    event.getCurrentPrice() != null ? event.getCurrentPrice() : "null",
                    event.getTimestamp() != null ? event.getTimestamp() : "null");
            
            if (event.getStrategyId() == 0) {
                log.error("StrategyNotificationEvent StrategyId is 0 (empty). Event data is invalid. Cannot send notification.");
                return;
            }
            
            if (event.getUserId() == 0) {
                log.error("StrategyNotificationEvent UserId is 0 (empty). Event data is invalid. StrategyId={}. Cannot send notification.", 
                        event.getStrategyId());
                return;
            }
            
            if (event.getAction() == null || event.getAction().trim().isEmpty()) {
                log.error("StrategyNotificationEvent Action is null or empty. Event data is invalid. StrategyId={}, UserId={}. Cannot send notification.", 
                        event.getStrategyId(), event.getUserId());
                return;
            }
            
            if (event.getUserEmail() == null || event.getUserEmail().trim().isEmpty()) {
                log.error("StrategyNotificationEvent userEmail is null or empty: StrategyId={}, UserId={}, Action={}. " +
                         "Email notification cannot be sent. Please ensure userEmail is properly set in the event.", 
                        event.getStrategyId(), event.getUserId(), event.getAction());
                return;
            }
            
            if (event.getStrategyName() == null || event.getStrategyName().trim().isEmpty()) {
                log.warn("StrategyNotificationEvent StrategyName is null or empty. StrategyId={}, UserId={}. Continuing with notification.", 
                        event.getStrategyId(), event.getUserId());
            }
            
            if (event.getStockSymbol() == null || event.getStockSymbol().trim().isEmpty()) {
                log.warn("StrategyNotificationEvent StockSymbol is null or empty. StrategyId={}, UserId={}. Continuing with notification.", 
                        event.getStrategyId(), event.getUserId());
            }
            
            log.info("Strategy Notification Event validated successfully: StrategyId={}, UserId={}, Action={}, Email={}, StrategyName={}, StockSymbol={}", 
                    event.getStrategyId(), event.getUserId(), event.getAction(), event.getUserEmail(),
                    event.getStrategyName() != null ? event.getStrategyName() : "N/A",
                    event.getStockSymbol() != null ? event.getStockSymbol() : "N/A");
            
            mailService.sendStrategyNotificationMail(event);
            log.info("Strategy notification mail sent successfully to: {}", event.getUserEmail());
        } catch (MessagingException e) {
            log.error("Error sending strategy notification mail to: {} - Error: {}", 
                    event != null ? event.getUserEmail() : "unknown", e.getMessage(), e);
            throw e;
        } catch (Exception e) {
            log.error("Unexpected error in handleStrategyNotificationEvent: StrategyId={}, UserId={}, Action={}, Error: {}", 
                    event != null ? event.getStrategyId() : "unknown",
                    event != null ? event.getUserId() : "unknown",
                    event != null ? event.getAction() : "unknown",
                    e.getMessage(), e);
            throw new MessagingException("Failed to send strategy notification mail", e);
        }
    }

    @RabbitListener(queues = RabbitMQConstants.notificationTradeCompletedQueue, containerFactory = "rabbitListenerContainerFactory")
    public void handleTradeCompletedEvent(@Payload TradeCompletedEvent event) throws MessagingException {
        try {
            if (event == null) {
                log.error("TradeCompletedEvent is null - skipping email notification");
                return;
            }
            
            if (event.getUserEmail() == null || event.getUserEmail().trim().isEmpty()) {
                log.error("TradeCompletedEvent userEmail is null or empty: AccountId={}, Symbol={}, TradeType={}. " +
                         "Email notification cannot be sent. Please ensure userEmail is properly set in the event.", 
                        event.getAccountId(), event.getSymbol(), event.getTradeType());
                return;
            }
            
            log.info("Trade Completed Event received: AccountId={}, Symbol={}, TradeType={}, Email={}", 
                    event.getAccountId(), event.getSymbol(), event.getTradeType(), event.getUserEmail());
            mailService.sendTradeCompletedMail(event);
            log.info("Trade completed mail sent successfully to: {}", event.getUserEmail());
        } catch (MessagingException e) {
            log.error("Error sending trade completed mail to: {} - Error: {}", 
                    event != null ? event.getUserEmail() : "unknown", e.getMessage(), e);
            throw e;
        } catch (Exception e) {
            log.error("Unexpected error in handleTradeCompletedEvent: AccountId={}, Error: {}", 
                    event != null ? event.getAccountId() : "unknown", e.getMessage(), e);
            throw new MessagingException("Failed to send trade completed mail", e);
        }
    }

    @RabbitListener(queues = RabbitMQConstants.notificationPaymentFailedQueue, containerFactory = "rabbitListenerContainerFactory")
    public void handlePaymentFailedEvent(@Payload PaymentFailedEvent event) throws MessagingException {
        try {
            if (event == null) {
                log.error("PaymentFailedEvent is null - skipping email notification");
                return;
            }
            
            if (event.getEmail() == null || event.getEmail().trim().isEmpty()) {
                log.error("PaymentFailedEvent email is null or empty: UserId={}, TransactionId={}. " +
                         "Email notification cannot be sent. Please ensure email is properly set in the event.", 
                        event.getUserId(), event.getPaymentTransactionId());
                return;
            }
            
            log.info("Payment Failed Event received: UserId={}, TransactionId={}, Amount={}, Email={}", 
                    event.getUserId(), event.getPaymentTransactionId(), event.getAmount(), event.getEmail());
            
            mailService.sendPaymentFailedMail(event);
            
            log.info("Payment failed mail sent successfully to: {}", event.getEmail());
        } catch (MessagingException e) {
            log.error("Error sending payment failed mail to: {} - Error: {}", 
                    event != null ? event.getEmail() : "unknown", e.getMessage(), e);
            throw e;
        } catch (Exception e) {
            log.error("Unexpected error in handlePaymentFailedEvent: UserId={}, Error: {}", 
                    event != null ? event.getUserId() : "unknown", e.getMessage(), e);
            throw new MessagingException("Failed to process PaymentFailedEvent", e);
        }
    }

    @RabbitListener(queues = RabbitMQConstants.notificationPaymentSuccessQueue, containerFactory = "rabbitListenerContainerFactory")
    public void handlePaymentSuccessEvent(@Payload PaymentSuccessEvent event) throws MessagingException {
        try {
            if (event == null) {
                log.error("PaymentSuccessEvent is null - skipping email notification");
                return;
            }
            
            if (event.getEmail() == null || event.getEmail().trim().isEmpty()) {
                log.error("PaymentSuccessEvent email is null or empty: UserId={}, AccountId={}, TransactionId={}. " +
                         "Email notification cannot be sent. Please ensure email is properly set in the event.", 
                        event.getUserId(), event.getAccountId(), event.getPaymentTransactionId());
                return;
            }
            
            log.info("Payment Success Event received: UserId={}, AccountId={}, TransactionId={}, Amount={}, Email={}", 
                    event.getUserId(), event.getAccountId(), event.getPaymentTransactionId(), 
                    event.getAmount(), event.getEmail());
            
            mailService.sendPaymentSuccessMail(event);
            
            log.info("Payment success mail sent successfully to: {}", event.getEmail());
        } catch (MessagingException e) {
            log.error("Error sending payment success mail to: {} - Error: {}", 
                    event != null ? event.getEmail() : "unknown", e.getMessage(), e);
            throw e;
        } catch (Exception e) {
            log.error("Unexpected error in handlePaymentSuccessEvent: UserId={}, AccountId={}, Error: {}", 
                    event != null ? event.getUserId() : "unknown", 
                    event != null ? event.getAccountId() : "unknown", 
                    e.getMessage(), e);
            throw new MessagingException("Failed to process PaymentSuccessEvent", e);
        }
    }

}
