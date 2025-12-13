package org.rtd.rtdnotificationservice.event;

public class OtpCreatedEvent {
    private String email;
    private String firstName;
    private String lastName;
    private String verifyOtpCode;
    private Long verifyOtpExpireAt;

    public OtpCreatedEvent() {
    }

    public OtpCreatedEvent(String email, Long verifyOtpExpireAt, String verifyOtpCode, String lastName, String firstName) {
        this.email = email;
        this.verifyOtpExpireAt = verifyOtpExpireAt;
        this.verifyOtpCode = verifyOtpCode;
        this.lastName = lastName;
        this.firstName = firstName;
    }

    public String getEmail() {
        return email;
    }

    public void setEmail(String email) {
        this.email = email;
    }

    public Long getVerifyOtpExpireAt() {
        return verifyOtpExpireAt;
    }

    public void setVerifyOtpExpireAt(Long verifyOtpExpireAt) {
        this.verifyOtpExpireAt = verifyOtpExpireAt;
    }

    public String getVerifyOtpCode() {
        return verifyOtpCode;
    }

    public void setVerifyOtpCode(String verifyOtpCode) {
        this.verifyOtpCode = verifyOtpCode;
    }

    public String getLastName() {
        return lastName;
    }

    public void setLastName(String lastName) {
        this.lastName = lastName;
    }

    public String getFirstName() {
        return firstName;
    }

    public void setFirstName(String firstName) {
        this.firstName = firstName;
    }
}

