package org.rtd.rtdnotificationservice.event;

public class OtpCreatedPasswordResetEvent {
    private String email;
    private String firstName;
    private String lastName;
    private String passwordResetOtpCode;
    private Long passwordResetOtpExpireAt;

    public OtpCreatedPasswordResetEvent() {
    }

    public OtpCreatedPasswordResetEvent(String email, String passwordResetOtpCode, String lastName, String firstName, Long passwordResetOtpExpireAt) {
        this.email = email;
        this.passwordResetOtpCode = passwordResetOtpCode;
        this.lastName = lastName;
        this.firstName = firstName;
        this.passwordResetOtpExpireAt = passwordResetOtpExpireAt;
    }

    public String getEmail() {
        return email;
    }

    public void setEmail(String email) {
        this.email = email;
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

    public String getPasswordResetOtpCode() {
        return passwordResetOtpCode;
    }

    public void setPasswordResetOtpCode(String passwordResetOtpCode) {
        this.passwordResetOtpCode = passwordResetOtpCode;
    }

    public Long getPasswordResetOtpExpireAt() {
        return passwordResetOtpExpireAt;
    }

    public void setPasswordResetOtpExpireAt(Long passwordResetOtpExpireAt) {
        this.passwordResetOtpExpireAt = passwordResetOtpExpireAt;
    }
}
