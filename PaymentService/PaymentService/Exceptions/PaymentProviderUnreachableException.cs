namespace PaymentService.Exceptions;
public class PaymentProviderUnreachableException : InvalidOperationException
{
    public string? Hostname { get; }
    public int? ErrorCode { get; }
    public string? DnsDiagnosticMessage { get; }
    public PaymentProviderUnreachableException(string message, string? hostname = null, int? errorCode = null, string? dnsDiagnosticMessage = null)
        : base(message)
    {
        Hostname = hostname;
        ErrorCode = errorCode;
        DnsDiagnosticMessage = dnsDiagnosticMessage;
    }
    public PaymentProviderUnreachableException(string message, Exception innerException, string? hostname = null, int? errorCode = null, string? dnsDiagnosticMessage = null)
        : base(message, innerException)
    {
        Hostname = hostname;
        ErrorCode = errorCode;
        DnsDiagnosticMessage = dnsDiagnosticMessage;
    }
}
