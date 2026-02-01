using KNQASelfService.Context;
using KNQASelfService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace KNQASelfService.Interfaces.UserManagement;

public interface IUserRepository
{
    Task AddAsync(User user);
    Task<IEnumerable<User>> GetAllAsync();
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailAsync(string email);
    Task UpdateAsync(User user);
    Task DeleteAsync(int id);
}

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<UserRepository>? _logger;

    // Constructor with logger (preferred - used when injected directly)
    public UserRepository(AppDbContext context, ILogger<UserRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Constructor without logger (for UnitOfWork)
    public UserRepository(AppDbContext context)
    {
        _context = context;
        _logger = null;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        _logger?.LogInformation($"🔍 Repository: Fetching user by email: {email}");

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user != null)
        {
            _logger?.LogInformation($"✅ Repository: User found");
            _logger?.LogInformation($"   UserId: {user.UserId}");
            _logger?.LogInformation($"   FullName: {user.FullName}");
            _logger?.LogInformation($"   UserNameId: '{user.UserNameId ?? "NULL"}'");
            _logger?.LogInformation($"   EmployeeNo: '{user.EmployeeNo ?? "NULL"}'");

            // Check if EmployeeNo is being loaded
            var employeeNoValue = user.EmployeeNo;
            _logger?.LogInformation($"   EmployeeNo IsNull: {employeeNoValue == null}");
            _logger?.LogInformation($"   EmployeeNo IsEmpty: {string.IsNullOrEmpty(employeeNoValue)}");
            _logger?.LogInformation($"   EmployeeNo Length: {employeeNoValue?.Length ?? 0}");
        }
        else
        {
            _logger?.LogWarning($"❌ Repository: User not found for email: {email}");
        }

        return user;
    }

    public async Task AddAsync(User user)
    {
        _logger?.LogInformation($"➕ Repository: Adding new user: {user.Email}");
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
        _logger?.LogInformation($"✅ Repository: User added successfully");
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        _logger?.LogInformation($"📋 Repository: Fetching all users");
        var users = await _context.Users
            .Include(u => u.Role)
            .ToListAsync();
        _logger?.LogInformation($"✅ Repository: Retrieved {users.Count()} users");
        return users;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        _logger?.LogInformation($"🔍 Repository: Fetching user by ID: {id}");
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == id);

        if (user != null)
        {
            _logger?.LogInformation($"✅ Repository: User found - {user.FullName}");
        }
        else
        {
            _logger?.LogWarning($"❌ Repository: User not found for ID: {id}");
        }

        return user;
    }

    public async Task UpdateAsync(User user)
    {
        _logger?.LogInformation($"✏️ Repository: Updating user: {user.Email}");
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        _logger?.LogInformation($"✅ Repository: User updated successfully");
    }

    public async Task DeleteAsync(int id)
    {
        _logger?.LogInformation($"🗑️ Repository: Deleting user with ID: {id}");
        var user = await GetByIdAsync(id);
        if (user != null)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            _logger?.LogInformation($"✅ Repository: User deleted successfully");
        }
        else
        {
            _logger?.LogWarning($"⚠️ Repository: Cannot delete - user not found");
        }
    }
}