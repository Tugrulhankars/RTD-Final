package org.rtd.rtdnotificationservice.dto.request;

public class CreateSmsRequest {
    private String smsTo;
    private String smsContent;
    private String smsTitle;
    private String smsFrom;

    public CreateSmsRequest(String smsTo, String smsFrom, String smsTitle, String smsContent) {
        this.smsTo = smsTo;
        this.smsFrom = smsFrom;
        this.smsTitle = smsTitle;
        this.smsContent = smsContent;
    }

    public CreateSmsRequest() {
    }

    public String getSmsTo() {
        return smsTo;
    }

    public void setSmsTo(String smsTo) {
        this.smsTo = smsTo;
    }

    public String getSmsFrom() {
        return smsFrom;
    }

    public void setSmsFrom(String smsFrom) {
        this.smsFrom = smsFrom;
    }

    public String getSmsTitle() {
        return smsTitle;
    }

    public void setSmsTitle(String smsTitle) {
        this.smsTitle = smsTitle;
    }

    public String getSmsContent() {
        return smsContent;
    }

    public void setSmsContent(String smsContent) {
        this.smsContent = smsContent;
    }
}
