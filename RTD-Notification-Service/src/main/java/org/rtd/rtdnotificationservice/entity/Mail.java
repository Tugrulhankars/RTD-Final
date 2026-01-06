package org.rtd.rtdnotificationservice.entity;

import jakarta.persistence.*;

@Entity
@Table(name = "mails")
public class Mail extends NotificationBase<Long>{

    private String mailTo;
    private String mailFrom;
    private String title;
    private String subject;

    public String getMailTo() {
        return mailTo;
    }

    public void setMailTo(String mailTo) {
        this.mailTo = mailTo;
    }

    public String getTitle() {
        return title;
    }

    public void setTitle(String title) {
        this.title = title;
    }

    public String getSubject() {
        return subject;
    }

    public void setSubject(String subject) {
        this.subject = subject;
    }

    public String getMailFrom() {
        return mailFrom;
    }

    public void setMailFrom(String mailFrom) {
        this.mailFrom = mailFrom;
    }
}
