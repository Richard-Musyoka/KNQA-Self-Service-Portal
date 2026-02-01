using KNQASelfService.Models;
using KNQASelfService.Interfaces.UserManagement;

public interface IUserAuthService
{
    Task<User?> AuthenticateAsync(string email, string password);
}

public class UserAuthService : IUserAuthService
{
    private readonly IUserRepository _userRepository;

    public UserAuthService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<User?> AuthenticateAsync(string email, string password)
    {
        var user = await _userRepository.GetByEmailAsync(email);

        if (user == null)
            return null;
        bool verified = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        if (verified)
            return user;

        return null;
    }
}
