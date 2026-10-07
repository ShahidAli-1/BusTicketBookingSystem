namespace BusTicketBookingSystem.Services
{
    public interface IPasswordService
    {
        string GenerateTemporaryPassword(int length = 10);
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
    }
}