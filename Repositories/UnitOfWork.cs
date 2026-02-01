using KNQASelfService.Context;
using KNQASelfService.Interfaces;
using KNQASelfService.Interfaces.UserManagement;
using KNQASelfService.Models;
namespace KNQASelfService.Repositories;
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
        userRepository = new UserRepository(_context);
        userTypes = new UserTypesRepo(_context);
        roleRepository = new RoleRepository(_context);
    }

    public IUserRepository userRepository { get; private set; }
    public IUserTypeRepository userTypes { get; private set; }
    public IRoleRepository roleRepository { get; private set; }

    public async Task<int> SaveAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}


