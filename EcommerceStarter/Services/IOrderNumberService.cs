namespace EcommerceStarter.Services
{
    public interface IOrderNumberService
    {
        string GenerateOrderNumber();
        Task<string> GenerateUniqueOrderNumberAsync(Func<string, Task<bool>> existsCheck);
    }
}
