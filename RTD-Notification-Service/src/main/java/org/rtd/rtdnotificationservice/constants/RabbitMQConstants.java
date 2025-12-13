package org.rtd.rtdnotificationservice.constants;

public  class RabbitMQConstants {
    public static final String  notificationDirectExchange="notification.exchange.direct";
    public  static  final  String notificationEmailOtpQueue="notification.otp.queue";
    public static final String notificationEmailOtpQueueKey="notification.otp.key";
    public static final String notificationEmailUserIsVerifyQueue="notification.user.is.verify.queue";
    public static final String notificationEmailUserIsVerifyKey="notification.user.is.verify.key";
    public static final String notificationEmailUserRegisterQueue="notification.user.register.queue";
    public static final String notificationEmailUserRegisterKey="notification.user.register.key";
    public static final String notificationPasswordResetOtpQueue="notification.password.reset.otp.queue";
    public static final String notificationPasswordResetOtpKey="notification.password.reset.otp.key";
    public static final String notificationStrategyNotificationQueue="notification.strategy.notification.queue";
    public static final String notificationStrategyNotificationKey="notification.strategy.notification.key";
    public static final String notificationTradeCompletedQueue="notification.trade.completed.queue";
    public static final String notificationTradeCompletedKey="notification.trade.completed.key";
    public static final String notificationPaymentFailedQueue="notification.payment.failed.queue";
    public static final String notificationPaymentFailedKey="notification.payment.failed.key";
    public static final String notificationPaymentSuccessQueue="notification.payment.success.queue";
    public static final String notificationPaymentSuccessKey="notification.payment.success.key";
}
