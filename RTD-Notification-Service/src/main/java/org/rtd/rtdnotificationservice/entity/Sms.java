package org.rtd.rtdnotificationservice.entity;

import jakarta.persistence.*;
import org.rtd.rtdnotificationservice.enums.NotificationType;

import java.time.LocalDateTime;

@Entity
@Table(name = "sms")
public class Sms extends NotificationBase<Long>{

    private String smsTo;
    private String smsFrom;
    private String smsTitle;
    private String smsContent;

    public Sms() {

    }
    public Sms(String smsTo, String smsContent, String smsTitle, String smsFrom) {
        this.smsTo = smsTo;
        this.smsContent = smsContent;
        this.smsTitle = smsTitle;
        this.smsFrom = smsFrom;
    }

    public Sms(Long aLong, NotificationType notificationType, LocalDateTime createdAt, LocalDateTime updatedAt, String createdBy, String updatedBy, String smsTo, String smsContent, String smsTitle, String smsFrom) {
        super(aLong, notificationType, createdAt, updatedAt, createdBy, updatedBy);
        this.smsTo = smsTo;
        this.smsContent = smsContent;
        this.smsTitle = smsTitle;
        this.smsFrom = smsFrom;
    }



    public String getSmsTo() {
        return smsTo;
    }

    public void setSmsTo(String smsTo) {
        this.smsTo = smsTo;
    }

    public String getSmsTitle() {
        return smsTitle;
    }

    public void setSmsTitle(String smsTitle) {
        this.smsTitle = smsTitle;
    }

    public String getSmsFrom() {
        return smsFrom;
    }

    public void setSmsFrom(String smsFrom) {
        this.smsFrom = smsFrom;
    }

    public String getSmsContent() {
        return smsContent;
    }

    public void setSmsContent(String smsContent) {
        this.smsContent = smsContent;
    }
}
