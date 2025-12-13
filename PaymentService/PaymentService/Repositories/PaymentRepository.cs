using Microsoft.EntityFrameworkCore;
using PaymentService.Models;
using System.Threading.Tasks;

namespace PaymentService.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly DatabaseContext _databaseContext;

    public PaymentRepository(DatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
    }

    public async Task CreatePayment(Payment payment)
    {
        await _databaseContext.AddAsync(payment);
        await _databaseContext.SaveChangesAsync();
        
    }

    public Task DeletePayment(Payment payment)
    {
        throw new NotImplementedException();
    }

    public async Task<List<Payment>> GetAllPaymentByUser(int userId)
    {
        List<Payment> payments = await _databaseContext.Set<Payment>().Where(p => p.UserId == userId).ToListAsync();
        return payments;
    }

    public Task<List<Payment>> GetAllPayments()
    {
        throw new NotImplementedException();
    }

    public Task<Payment> GetPaymentById(int id)
    {
        throw new NotImplementedException();
    }

    public Task<Payment> UpdatePayment(Payment payment)
    {
        throw new NotImplementedException();
    }
}
