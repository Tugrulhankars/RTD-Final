package org.rtd.rtdnotificationservice.service;

import jakarta.mail.MessagingException;
import org.rtd.rtdnotificationservice.event.OtpCreatedEvent;
import org.rtd.rtdnotificationservice.event.OtpCreatedPasswordResetEvent;
import org.rtd.rtdnotificationservice.event.OtpVerifiedEvent;
import org.rtd.rtdnotificationservice.event.PaymentFailedEvent;
import org.rtd.rtdnotificationservice.event.PaymentSuccessEvent;
import org.rtd.rtdnotificationservice.event.StrategyNotificationEvent;
import org.rtd.rtdnotificationservice.event.TradeCompletedEvent;
import org.rtd.rtdnotificationservice.event.UserRegisteredEvent;

public interface MailService {

    void sendOtpMail(OtpCreatedEvent event) throws MessagingException;
    void sendWelcomeMail(UserRegisteredEvent event) throws MessagingException;
    void sendUserVerifiedMail(OtpVerifiedEvent event) throws MessagingException;
    void sendPasswordResetOtpMail(OtpCreatedPasswordResetEvent event) throws MessagingException;
    void sendStrategyNotificationMail(StrategyNotificationEvent event) throws MessagingException;
    void sendTradeCompletedMail(TradeCompletedEvent event) throws MessagingException;
    void sendPaymentFailedMail(PaymentFailedEvent event) throws MessagingException;
    void sendPaymentSuccessMail(PaymentSuccessEvent event) throws MessagingException;
}
