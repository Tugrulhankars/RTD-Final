package org.rtd.rtdnotificationservice.config;

import org.rtd.rtdnotificationservice.constants.RabbitMQConstants;
import org.springframework.amqp.core.Binding;
import org.springframework.amqp.core.BindingBuilder;
import org.springframework.amqp.core.DirectExchange;
import org.springframework.amqp.core.Queue;
import org.springframework.amqp.rabbit.config.SimpleRabbitListenerContainerFactory;
import org.springframework.amqp.rabbit.connection.CachingConnectionFactory;
import org.springframework.amqp.rabbit.connection.ConnectionFactory;
import org.springframework.amqp.rabbit.core.RabbitAdmin;
import org.springframework.amqp.rabbit.listener.RabbitListenerContainerFactory;
import org.springframework.amqp.rabbit.listener.SimpleMessageListenerContainer;
import org.springframework.amqp.core.QueueBuilder;
import org.springframework.amqp.rabbit.listener.ConditionalRejectingErrorHandler;
import org.springframework.amqp.rabbit.listener.FatalExceptionStrategy;
import org.springframework.amqp.support.converter.DefaultJackson2JavaTypeMapper;
import org.springframework.amqp.support.converter.Jackson2JsonMessageConverter;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.util.ErrorHandler;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.SerializationFeature;
import com.fasterxml.jackson.datatype.jsr310.JavaTimeModule;

import java.util.HashMap;
import java.util.Map;

@Configuration
public class RabbitMqConfig {

    @Bean
    public DirectExchange directExchange(){
        return new DirectExchange(RabbitMQConstants.notificationDirectExchange, true, false);
    }

    @Bean
    public Jackson2JsonMessageConverter jsonMessageConverter(){
        ObjectMapper objectMapper = new ObjectMapper();
        objectMapper.registerModule(new JavaTimeModule());
        objectMapper.disable(SerializationFeature.WRITE_DATES_AS_TIMESTAMPS);
        
        Jackson2JsonMessageConverter converter = new Jackson2JsonMessageConverter(objectMapper);

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
        
        factory.setErrorHandler(errorHandler());
        
        factory.setDefaultRequeueRejected(false);
        
        return factory;
    }
    
    @Bean
    public ErrorHandler errorHandler() {
        return new ConditionalRejectingErrorHandler(new FatalExceptionStrategy() {
            @Override
            public boolean isFatal(Throwable t) {
                return true;
            }
        });
    }

    @Value("${spring.rabbitmq.uri}")
    private String rabbitMqUri;
    
    @Bean
    public ConnectionFactory connectionFactory(){
        CachingConnectionFactory connectionFactory = new CachingConnectionFactory();
        connectionFactory.setUri(rabbitMqUri);
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
    public Binding otpQueueBinding(Queue otpQueue, DirectExchange directExchange) {
        return BindingBuilder.bind(otpQueue).to(directExchange).with(RabbitMQConstants.notificationEmailOtpQueueKey);
    }
    
    @Bean
    public Queue userRegisterQueue() {
        return new Queue(RabbitMQConstants.notificationEmailUserRegisterQueue, true, false, false);
    }
    
    @Bean
    public Binding userRegisterQueueBinding(Queue userRegisterQueue, DirectExchange directExchange) {
        return BindingBuilder.bind(userRegisterQueue).to(directExchange).with(RabbitMQConstants.notificationEmailUserRegisterKey);
    }
    
    @Bean
    public Queue userIsVerifyQueue() {
        return new Queue(RabbitMQConstants.notificationEmailUserIsVerifyQueue, true, false, false);
    }
    
    @Bean
    public Binding userIsVerifyQueueBinding(Queue userIsVerifyQueue, DirectExchange directExchange) {
        return BindingBuilder.bind(userIsVerifyQueue).to(directExchange).with(RabbitMQConstants.notificationEmailUserIsVerifyKey);
    }
    
    @Bean
    public Queue passwordResetOtpQueue() {
        return new Queue(RabbitMQConstants.notificationPasswordResetOtpQueue, true, false, false);
    }
    
    @Bean
    public Binding passwordResetOtpQueueBinding(Queue passwordResetOtpQueue, DirectExchange directExchange) {
        return BindingBuilder.bind(passwordResetOtpQueue).to(directExchange).with(RabbitMQConstants.notificationPasswordResetOtpKey);
    }
    
    @Bean
    public Queue strategyNotificationQueue() {
        return new Queue(RabbitMQConstants.notificationStrategyNotificationQueue, true, false, false);
    }
    
    @Bean
    public Binding strategyNotificationQueueBinding(Queue strategyNotificationQueue, DirectExchange directExchange) {
        return BindingBuilder.bind(strategyNotificationQueue).to(directExchange).with(RabbitMQConstants.notificationStrategyNotificationKey);
    }
    
    @Bean
    public Queue tradeCompletedQueue() {
        return new Queue(RabbitMQConstants.notificationTradeCompletedQueue, true, false, false);
    }
    
    @Bean
    public Binding tradeCompletedQueueBinding(Queue tradeCompletedQueue, DirectExchange directExchange) {
        return BindingBuilder.bind(tradeCompletedQueue).to(directExchange).with(RabbitMQConstants.notificationTradeCompletedKey);
    }
    
    @Bean
    public Queue paymentFailedQueue() {
        return QueueBuilder.durable(RabbitMQConstants.notificationPaymentFailedQueue)
                .withArgument("x-dead-letter-exchange", RabbitMQConstants.notificationDirectExchange)
                .withArgument("x-dead-letter-routing-key", RabbitMQConstants.notificationPaymentFailedDlqKey)
                .withArgument("x-message-ttl", 60000)
                .build();
    }
    
    @Bean
    public Binding paymentFailedQueueBinding(Queue paymentFailedQueue, DirectExchange directExchange) {
        return BindingBuilder.bind(paymentFailedQueue).to(directExchange).with(RabbitMQConstants.notificationPaymentFailedKey);
    }
    
    @Bean
    public Queue paymentFailedDlq() {
        return new Queue(RabbitMQConstants.notificationPaymentFailedDlq, true, false, false);
    }
    
    @Bean
    public Binding paymentFailedDlqBinding(Queue paymentFailedDlq, DirectExchange directExchange) {
        return BindingBuilder.bind(paymentFailedDlq).to(directExchange).with(RabbitMQConstants.notificationPaymentFailedDlqKey);
    }
    
    @Bean
    public Queue paymentSuccessQueue() {
        return QueueBuilder.durable(RabbitMQConstants.notificationPaymentSuccessQueue)
                .withArgument("x-dead-letter-exchange", RabbitMQConstants.notificationDirectExchange)
                .withArgument("x-dead-letter-routing-key", RabbitMQConstants.notificationPaymentSuccessDlqKey)
                .withArgument("x-message-ttl", 60000)
                .build();
    }
    
    @Bean
    public Binding paymentSuccessQueueBinding(Queue paymentSuccessQueue, DirectExchange directExchange) {
        return BindingBuilder.bind(paymentSuccessQueue).to(directExchange).with(RabbitMQConstants.notificationPaymentSuccessKey);
    }
    
    @Bean
    public Queue paymentSuccessDlq() {
        return new Queue(RabbitMQConstants.notificationPaymentSuccessDlq, true, false, false);
    }
    
    @Bean
    public Binding paymentSuccessDlqBinding(Queue paymentSuccessDlq, DirectExchange directExchange) {
        return BindingBuilder.bind(paymentSuccessDlq).to(directExchange).with(RabbitMQConstants.notificationPaymentSuccessDlqKey);
    }
}
