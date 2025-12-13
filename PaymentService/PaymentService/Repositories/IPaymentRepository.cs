using PaymentService.Models;

namespace PaymentService.Repositories;

public interface IPaymentRepository
{

    public Task CreatePayment(Payment payment);
    public Task<Payment> UpdatePayment(Payment payment);
    public Task DeletePayment(Payment payment);
    public Task<Payment> GetPaymentById(int id);
    public Task<List<Payment>> GetAllPayments();
    public Task<List<Payment>> GetAllPaymentByUser(int userId);
}
