package org.rtd.rtdnotificationservice.dto.request;

public class CreateMailRequest {
    private String mailTo;
    private String mailContent;
    private String mailTitle;
    private String mailFrom;

    public CreateMailRequest(String mailTo, String mailFrom, String mailTitle, String mailContent) {
        this.mailTo = mailTo;
        this.mailFrom = mailFrom;
        this.mailTitle = mailTitle;
        this.mailContent = mailContent;
    }

    public CreateMailRequest() {
    }

    public String getMailTo() {
        return mailTo;
    }

    public void setMailTo(String mailTo) {
        this.mailTo = mailTo;
    }

    public String getMailFrom() {
        return mailFrom;
    }

    public void setMailFrom(String mailFrom) {
        this.mailFrom = mailFrom;
    }

    public String getMailTitle() {
        return mailTitle;
    }

    public void setMailTitle(String mailTitle) {
        this.mailTitle = mailTitle;
    }

    public String getMailContent() {
        return mailContent;
    }

    public void setMailContent(String mailContent) {
        this.mailContent = mailContent;
    }
}
