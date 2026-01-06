package org.rtd.rtdnotificationservice.service.impl;

import jakarta.mail.MessagingException;
import jakarta.mail.internet.MimeMessage;
import org.rtd.rtdnotificationservice.entity.Mail;
import org.rtd.rtdnotificationservice.enums.NotificationType;
import org.rtd.rtdnotificationservice.event.OtpCreatedEvent;
import org.rtd.rtdnotificationservice.event.OtpCreatedPasswordResetEvent;
import org.rtd.rtdnotificationservice.event.OtpVerifiedEvent;
import org.rtd.rtdnotificationservice.event.PaymentFailedEvent;
import org.rtd.rtdnotificationservice.event.PaymentSuccessEvent;
import org.rtd.rtdnotificationservice.event.StrategyNotificationEvent;
import org.rtd.rtdnotificationservice.event.TradeCompletedEvent;
import org.rtd.rtdnotificationservice.event.UserRegisteredEvent;
import org.rtd.rtdnotificationservice.repository.MailRepository;
import org.rtd.rtdnotificationservice.service.MailService;
import org.springframework.mail.SimpleMailMessage;
import org.springframework.mail.javamail.JavaMailSender;
import org.springframework.mail.javamail.MimeMessageHelper;
import org.springframework.stereotype.Service;
import org.thymeleaf.TemplateEngine;
import org.thymeleaf.context.Context;

@Service
public class MailServiceImpl implements MailService {
    private final MailRepository mailRepository;
    private final JavaMailSender javaMailSender;
    private final TemplateEngine templateEngine;

    public MailServiceImpl(MailRepository mailRepository, JavaMailSender javaMailSender, TemplateEngine templateEngine) {
        this.mailRepository = mailRepository;
        this.javaMailSender = javaMailSender;
        this.templateEngine = templateEngine;
    }

    @Override
    public void sendOtpMail(OtpCreatedEvent event) throws MessagingException {
        try {
            System.out.println("Starting to send OTP mail to: " + event.getEmail() + ", OTP: " + event.getVerifyOtpCode());
            Mail mail=new Mail();
            Context context = new Context();
            context.setVariable("OTP",event.getVerifyOtpCode());
            String process=templateEngine.process("email-otp",context);
            System.out.println("Email template processed successfully");
            
            MimeMessage mimeMessage=javaMailSender.createMimeMessage();
            MimeMessageHelper helper=new MimeMessageHelper(mimeMessage);
            helper.setFrom("karslitugrulhan@gmail.com");
            helper.setTo(event.getEmail());
            helper.setSubject("OTP Doğrulama");
            helper.setText(process,true);
            System.out.println("MimeMessage prepared, sending email...");
            
            javaMailSender.send(mimeMessage);
            System.out.println("Email sent successfully to: " + event.getEmail());

            mail.setMailTo(event.getEmail());
            mail.setMailFrom("karslitugrulhan@gmail.com");
            mail.setTitle("OTP Doğrulama");
            mail.setSubject(event.getVerifyOtpCode());
            mail.setNotificationType(NotificationType.MAIL);
            mailRepository.save(mail);
            System.out.println("Mail record saved to database");
        } catch (Exception e) {
            System.err.println("Error in sendOtpMail: " + e.getMessage());
            e.printStackTrace();
            throw e;
        }
    }

    @Override
    public void sendWelcomeMail(UserRegisteredEvent event) throws MessagingException {
        Mail mail=new Mail();
        Context context = new Context();
        context.setVariable("FirstName",event.getFirstName());
        context.setVariable("LastName",event.getLastName());

        String process=templateEngine.process("welcome",context);
        MimeMessage mimeMessage=javaMailSender.createMimeMessage();
        MimeMessageHelper helper=new MimeMessageHelper(mimeMessage);
        helper.setFrom("karslitugrulhan@gmail.com");
        helper.setTo(event.getEmail());
        helper.setSubject("Hoşgeldiniz");
        helper.setText(process,true);
        javaMailSender.send(mimeMessage);

        mail.setMailFrom("karslitugrulhan@gmail.com");
        mail.setMailTo(event.getEmail());
        mail.setTitle("Hoşgeldiniz");
        mail.setSubject("Hoşgeldiniz");
        mail.setNotificationType(NotificationType.MAIL);
        mailRepository.save(mail);

    }

    @Override
    public void sendUserVerifiedMail(OtpVerifiedEvent event) throws MessagingException {
        Mail mail=new Mail();
        Context context = new Context();
        context.setVariable("FirstName",event.getFirstName());
        context.setVariable("LastName",event.getLastName());

        String process=templateEngine.process("user-verified",context);
        MimeMessage mimeMessage=javaMailSender.createMimeMessage();
        MimeMessageHelper helper=new MimeMessageHelper(mimeMessage);
        helper.setFrom("karslitugrulhan@gmail.com");
        helper.setTo(event.getEmail());
        helper.setSubject("Hesabınız Onaylandı");
        helper.setText(process,true);
        javaMailSender.send(mimeMessage);

        mail.setMailFrom("karslitugrulhan@gmail.com");
        mail.setMailTo(event.getEmail());
        mail.setTitle("Hesabınız Onaylandı");
        mail.setSubject("Hesabınız Onaylandı");
        mail.setNotificationType(NotificationType.MAIL);
        mailRepository.save(mail);

    }

    @Override
    public void sendPasswordResetOtpMail(OtpCreatedPasswordResetEvent event) throws MessagingException {

    }

    @Override
    public void sendStrategyNotificationMail(StrategyNotificationEvent event) throws MessagingException {
        Mail mail = new Mail();
        Context context = new Context();
        
        context.setVariable("strategyName", event.getStrategyName());
        context.setVariable("stockSymbol", event.getStockSymbol());
        context.setVariable("status", event.getStatus());
        context.setVariable("action", event.getAction());
        context.setVariable("buyPrice", event.getBuyPrice() != null ? event.getBuyPrice().toString() : "N/A");
        context.setVariable("sellPrice", event.getSellPrice() != null ? event.getSellPrice().toString() : "N/A");
        String profitLossStr = event.getProfitLoss() != null ? event.getProfitLoss().toString() : "N/A";
        context.setVariable("profitLoss", profitLossStr);
        context.setVariable("profitLossValue", event.getProfitLoss());
        context.setVariable("currentPrice", event.getCurrentPrice() != null ? event.getCurrentPrice().toString() : "N/A");
        context.setVariable("timestamp", event.getTimestamp() != null ? event.getTimestamp().toString() : "N/A");
        context.setVariable("reason", event.getReason() != null ? event.getReason() : "");
        context.setVariable("executedRules", event.getExecutedRules());

        String process = templateEngine.process("strategy-notification", context);
        MimeMessage mimeMessage = javaMailSender.createMimeMessage();
        MimeMessageHelper helper = new MimeMessageHelper(mimeMessage);
        helper.setFrom("karslitugrulhan@gmail.com");
        
        String email = event.getUserEmail();
        if (email == null || email.isEmpty()) {
            throw new MessagingException("User email is required but not provided in event");
        }
        
        helper.setTo(email);
        helper.setSubject("Strateji Bildirimi: " + event.getStrategyName() + " - " + event.getAction());
        helper.setText(process, true);
        javaMailSender.send(mimeMessage);

        mail.setMailFrom("karslitugrulhan@gmail.com");
        mail.setMailTo(email);
        mail.setTitle("Strateji Bildirimi: " + event.getStrategyName());
        mail.setSubject("Strateji: " + event.getStrategyName() + " - " + event.getAction() + " - " + event.getStockSymbol());
        mail.setNotificationType(NotificationType.MAIL);
        mailRepository.save(mail);
    }

    @Override
    public void sendTradeCompletedMail(TradeCompletedEvent event) throws MessagingException {
        Mail mail = new Mail();
        Context context = new Context();
        
        context.setVariable("symbol", event.getSymbol());
        context.setVariable("tradeType", event.getTradeType() != null ? event.getTradeType().name() : "N/A");
        context.setVariable("quantity", event.getQuantity() != null ? event.getQuantity().toString() : "N/A");
        context.setVariable("price", event.getPrice() != null ? event.getPrice().toString() : "N/A");
        context.setVariable("total", event.getTotal() != null ? event.getTotal().toString() : "N/A");
        context.setVariable("executedAt", event.getExecutedAt() != null ? event.getExecutedAt().toString() : "N/A");
        context.setVariable("accountId", event.getAccountId());

        String process = templateEngine.process("trade-completed", context);
        MimeMessage mimeMessage = javaMailSender.createMimeMessage();
        MimeMessageHelper helper = new MimeMessageHelper(mimeMessage);
        helper.setFrom("karslitugrulhan@gmail.com");
        
        String email = event.getUserEmail();
        if (email == null || email.isEmpty()) {
            throw new MessagingException("User email is required but not provided in event");
        }
        
        helper.setTo(email);
        helper.setSubject("İşlem Tamamlandı: " + event.getSymbol() + " - " + event.getTradeType());
        helper.setText(process, true);
        javaMailSender.send(mimeMessage);

        mail.setMailFrom("karslitugrulhan@gmail.com");
        mail.setMailTo(email);
        mail.setTitle("İşlem Tamamlandı: " + event.getSymbol());
        mail.setSubject("İşlem: " + event.getTradeType() + " - " + event.getSymbol() + " - " + event.getQuantity() + " adet");
        mail.setNotificationType(NotificationType.MAIL);
        mailRepository.save(mail);
    }

    @Override
    public void sendPaymentFailedMail(PaymentFailedEvent event) throws MessagingException {
        Mail mail = new Mail();
        Context context = new Context();
        
        context.setVariable("amount", event.getAmount() != null ? event.getAmount().toString() : "N/A");
        context.setVariable("currency", event.getCurrency() != null ? event.getCurrency() : "TRY");
        context.setVariable("paymentTransactionId", event.getPaymentTransactionId() != null ? event.getPaymentTransactionId() : "N/A");
        context.setVariable("paymentMethod", event.getPaymentMethod() != null ? event.getPaymentMethod() : "N/A");
        context.setVariable("paymentDate", event.getPaymentDate() != null ? event.getPaymentDate().toString() : "N/A");
        context.setVariable("failureReason", event.getFailureReason() != null ? event.getFailureReason() : "");
        context.setVariable("errorCode", event.getErrorCode() != null ? event.getErrorCode() : "");
        context.setVariable("errorMessage", event.getErrorMessage() != null ? event.getErrorMessage() : "");
        context.setVariable("userId", event.getUserId());

        String process = templateEngine.process("payment-failed", context);
        MimeMessage mimeMessage = javaMailSender.createMimeMessage();
        MimeMessageHelper helper = new MimeMessageHelper(mimeMessage);
        helper.setFrom("karslitugrulhan@gmail.com");
        
        String email = event.getEmail();
        if (email == null || email.isEmpty()) {
            throw new MessagingException("User email is required but not provided in event");
        }
        
        helper.setTo(email);
        helper.setSubject("Ödeme Başarısız - İşlem ID: " + event.getPaymentTransactionId());
        helper.setText(process, true);
        javaMailSender.send(mimeMessage);

        mail.setMailFrom("karslitugrulhan@gmail.com");
        mail.setMailTo(email);
        mail.setTitle("Ödeme Başarısız");
        mail.setSubject("Ödeme Başarısız - " + event.getAmount() + " " + event.getCurrency());
        mail.setNotificationType(NotificationType.MAIL);
        mailRepository.save(mail);
    }

    @Override
    public void sendPaymentSuccessMail(PaymentSuccessEvent event) throws MessagingException {
        Mail mail = new Mail();
        Context context = new Context();
        
        context.setVariable("amount", event.getAmount() != null ? event.getAmount().toString() : "N/A");
        context.setVariable("currency", event.getCurrency() != null ? event.getCurrency() : "TRY");
        context.setVariable("paymentTransactionId", event.getPaymentTransactionId() != null ? event.getPaymentTransactionId() : "N/A");
        context.setVariable("paymentMethod", event.getPaymentMethod() != null ? event.getPaymentMethod() : "N/A");
        context.setVariable("paymentDate", event.getPaymentDate() != null ? event.getPaymentDate().toString() : "N/A");
        context.setVariable("message", event.getMessage() != null ? event.getMessage() : "");
        context.setVariable("userId", event.getUserId());
        context.setVariable("accountId", event.getAccountId());

        String process = templateEngine.process("payment-success", context);
        MimeMessage mimeMessage = javaMailSender.createMimeMessage();
        MimeMessageHelper helper = new MimeMessageHelper(mimeMessage);
        helper.setFrom("karslitugrulhan@gmail.com");
        
        String email = event.getEmail();
        if (email == null || email.isEmpty()) {
            throw new MessagingException("User email is required but not provided in event");
        }
        
        helper.setTo(email);
        helper.setSubject("Ödeme Başarılı - İşlem ID: " + event.getPaymentTransactionId());
        helper.setText(process, true);
        javaMailSender.send(mimeMessage);

        mail.setMailFrom("karslitugrulhan@gmail.com");
        mail.setMailTo(email);
        mail.setTitle("Ödeme Başarılı");
        mail.setSubject("Ödeme Başarılı - " + event.getAmount() + " " + event.getCurrency());
        mail.setNotificationType(NotificationType.MAIL);
        mailRepository.save(mail);
    }
}
