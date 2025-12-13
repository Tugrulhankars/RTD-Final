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
            log.warn("Mail Event Listener: {}",event);
            mailService.sendOtpMail(event);
        }catch (MessagingException e) {
            throw e;
        }
    }

    @RabbitListener(queues = RabbitMQConstants.notificationEmailUserRegisterQueue, containerFactory = "rabbitListenerContainerFactory")
    public void handleUserRegisteredEvent(@Payload UserRegisteredEvent event) throws MessagingException, JsonProcessingException {
        try {
            mailService.sendWelcomeMail(event);
        }catch (MessagingException e) {
            throw  e;
        }
    }

    @RabbitListener(queues = RabbitMQConstants.notificationEmailUserIsVerifyQueue, containerFactory = "rabbitListenerContainerFactory")
    public void handleOtpVerifiedEvent(@Payload OtpVerifiedEvent event) throws MessagingException {
        try {
            mailService.sendUserVerifiedMail(event);
        }catch (MessagingException e) {
            throw  e;
        }
    }

    @RabbitListener(queues = RabbitMQConstants.notificationPasswordResetOtpQueue, containerFactory = "rabbitListenerContainerFactory")
    public void handlePasswordResetOtpEvent(@Payload OtpCreatedPasswordResetEvent event) throws MessagingException {
        try {
            mailService.sendPasswordResetOtpMail(event);
        } catch (MessagingException e) {
            throw e;
        }
    }

    @RabbitListener(queues = RabbitMQConstants.notificationStrategyNotificationQueue, containerFactory = "rabbitListenerContainerFactory")
    public void handleStrategyNotificationEvent(@Payload StrategyNotificationEvent event) throws MessagingException {
        try {
            log.info("Strategy Notification Event received: StrategyId={}, UserId={}, Action={}", 
                    event.getStrategyId(), event.getUserId(), event.getAction());
            mailService.sendStrategyNotificationMail(event);
        } catch (MessagingException e) {
            log.error("Error sending strategy notification mail", e);
            throw e;
        }
    }

    @RabbitListener(queues = RabbitMQConstants.notificationTradeCompletedQueue, containerFactory = "rabbitListenerContainerFactory")
    public void handleTradeCompletedEvent(@Payload TradeCompletedEvent event) throws MessagingException {
        try {
            log.info("Trade Completed Event received: AccountId={}, Symbol={}, TradeType={}", 
                    event.getAccountId(), event.getSymbol(), event.getTradeType());
            mailService.sendTradeCompletedMail(event);
        } catch (MessagingException e) {
            log.error("Error sending trade completed mail", e);
            throw e;
        }
    }

    @RabbitListener(queues = RabbitMQConstants.notificationPaymentFailedQueue, containerFactory = "rabbitListenerContainerFactory")
    public void handlePaymentFailedEvent(@Payload PaymentFailedEvent event) throws MessagingException {
        try {
            log.info("Payment Failed Event received: UserId={}, TransactionId={}, Amount={}", 
                    event.getUserId(), event.getPaymentTransactionId(), event.getAmount());
            mailService.sendPaymentFailedMail(event);
        } catch (MessagingException e) {
            log.error("Error sending payment failed mail", e);
            throw e;
        }
    }

    @RabbitListener(queues = RabbitMQConstants.notificationPaymentSuccessQueue, containerFactory = "rabbitListenerContainerFactory")
    public void handlePaymentSuccessEvent(@Payload PaymentSuccessEvent event) throws MessagingException {
        try {
            log.info("Payment Success Event received: UserId={}, AccountId={}, TransactionId={}, Amount={}", 
                    event.getUserId(), event.getAccountId(), event.getPaymentTransactionId(), event.getAmount());
            mailService.sendPaymentSuccessMail(event);
        } catch (MessagingException e) {
            log.error("Error sending payment success mail", e);
            throw e;
        }
    }

}
