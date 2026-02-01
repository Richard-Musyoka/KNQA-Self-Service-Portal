using KNQASelfService.Models;
using KNQASelfService.Interfaces.UserManagement;

namespace KNQASelfService.Interfaces;
public interface IUnitOfWork : IDisposable
{
    IUserRepository userRepository { get; }

    IUserTypeRepository userTypes { get; }
    IRoleRepository roleRepository { get; }
    Task<int> SaveAsync();
}

