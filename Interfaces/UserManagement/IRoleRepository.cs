using KNQASelfService.Models;

namespace KNQASelfService.Interfaces.UserManagement
{
    public interface IRoleRepository
    {
        Task<IEnumerable<Role>> GetAllAsync();
        Task<Role> GetByIdAsync(int id);
    }
}