package org.rtd.rtdnotificationservice.config;

import org.rtd.rtdnotificationservice.constants.RabbitMQConstants;
import org.springframework.amqp.core.Queue;
import org.springframework.amqp.rabbit.config.SimpleRabbitListenerContainerFactory;
import org.springframework.amqp.rabbit.connection.CachingConnectionFactory;
import org.springframework.amqp.rabbit.connection.ConnectionFactory;
import org.springframework.amqp.rabbit.core.RabbitAdmin;
import org.springframework.amqp.rabbit.listener.RabbitListenerContainerFactory;
import org.springframework.amqp.rabbit.listener.SimpleMessageListenerContainer;
import org.springframework.amqp.support.converter.DefaultJackson2JavaTypeMapper;
import org.springframework.amqp.support.converter.Jackson2JsonMessageConverter;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

import java.util.HashMap;
import java.util.Map;

@Configuration
public class RabbitMqConfig {

    @Bean
    public Jackson2JsonMessageConverter jsonMessageConverter(){
        Jackson2JsonMessageConverter converter = new Jackson2JsonMessageConverter();

        DefaultJackson2JavaTypeMapper typeMapper = new DefaultJackson2JavaTypeMapper();

        Map<String, Class<?>> idClassMapping = new HashMap<>();

        idClassMapping.put(
                "org.rtd.rtdauthuserservice.events.OtpCreatedEvent",
                org.rtd.rtdnotificationservice.event.OtpCreatedEvent.class
        );
        idClassMapping.put(
                "org.rtd.rtdauthuserservice.events.OtpVerifiedEvent",
                org.rtd.rtdnotificationservice.event.OtpVerifiedEvent.class
        );
        idClassMapping.put(
                "org.rtd.rtdauthuserservice.events.UserRegisteredEvent",
                org.rtd.rtdnotificationservice.event.UserRegisteredEvent.class
        );
        idClassMapping.put(
                "org.rtd.rtdauthuserservice.events.OtpVerifiedEvent",
                org.rtd.rtdnotificationservice.event.OtpVerifiedEvent.class
        );
        idClassMapping.put(
                "org.rtd.rtdstrategyruleservice.events.StrategyNotificationEvent",
                org.rtd.rtdnotificationservice.event.StrategyNotificationEvent.class
        );
        idClassMapping.put(
                "org.rtd.rtdtradingservice.events.TradeCompletedEvent",
                org.rtd.rtdnotificationservice.event.TradeCompletedEvent.class
        );
        idClassMapping.put(
                "org.rtd.rtdpaymentservice.events.PaymentFailedEvent",
                org.rtd.rtdnotificationservice.event.PaymentFailedEvent.class
        );
        idClassMapping.put(
                "org.rtd.rtdpaymentservice.events.PaymentSuccessEvent",
                org.rtd.rtdnotificationservice.event.PaymentSuccessEvent.class
        );

        typeMapper.setIdClassMapping(idClassMapping);
        converter.setJavaTypeMapper(typeMapper);
        return converter;
    }

    @Bean
    public RabbitListenerContainerFactory<SimpleMessageListenerContainer> rabbitListenerContainerFactory(){
        SimpleRabbitListenerContainerFactory factory = new SimpleRabbitListenerContainerFactory();
        factory.setConnectionFactory(connectionFactory());
        factory.setConcurrentConsumers(2);
        factory.setMaxConcurrentConsumers(4);
        factory.setPrefetchCount(5);
        factory.setGlobalQos(true);
        factory.setMessageConverter(jsonMessageConverter());
        factory.setMissingQueuesFatal(false);
        return factory;
    }

    @Bean
    public ConnectionFactory connectionFactory(){
        CachingConnectionFactory connectionFactory = new CachingConnectionFactory();
        connectionFactory.setUri("amqps://okzwdbrz:AmGKgw5DTXuIAjOraNCNzFiqI5_lhV-s@kebnekaise.lmq.cloudamqp.com/okzwdbrz");
        connectionFactory.setPublisherReturns(true);
        return connectionFactory;
    }
    
    @Bean
    public RabbitAdmin rabbitAdmin(ConnectionFactory connectionFactory) {
        RabbitAdmin admin = new RabbitAdmin(connectionFactory);
        admin.setAutoStartup(true);
        admin.setIgnoreDeclarationExceptions(false);
        return admin;
    }
    
    @Bean
    public Queue otpQueue() {
        return new Queue(RabbitMQConstants.notificationEmailOtpQueue, true, false, false);
    }
    
    @Bean
    public Queue userRegisterQueue() {
        return new Queue(RabbitMQConstants.notificationEmailUserRegisterQueue, true, false, false);
    }
    
    @Bean
    public Queue userIsVerifyQueue() {
        return new Queue(RabbitMQConstants.notificationEmailUserIsVerifyQueue, true, false, false);
    }
    
    @Bean
    public Queue passwordResetOtpQueue() {
        return new Queue(RabbitMQConstants.notificationPasswordResetOtpQueue, true, false, false);
    }
    
    @Bean
    public Queue strategyNotificationQueue() {
        return new Queue(RabbitMQConstants.notificationStrategyNotificationQueue, true, false, false);
    }
    
    @Bean
    public Queue tradeCompletedQueue() {
        return new Queue(RabbitMQConstants.notificationTradeCompletedQueue, true, false, false);
    }
    
    @Bean
    public Queue paymentFailedQueue() {
        return new Queue(RabbitMQConstants.notificationPaymentFailedQueue, true, false, false);
    }
    
    @Bean
    public Queue paymentSuccessQueue() {
        return new Queue(RabbitMQConstants.notificationPaymentSuccessQueue, true, false, false);
    }
}
