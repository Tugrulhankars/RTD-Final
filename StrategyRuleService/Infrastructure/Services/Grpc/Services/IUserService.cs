namespace Infrastructure.Services.Grpc.Services;
public interface IUserService
{
    Task<string?> GetUserEmailByIdAsync(int userId);
}
