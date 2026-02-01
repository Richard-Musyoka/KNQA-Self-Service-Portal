using Microsoft.EntityFrameworkCore;
using KNQASelfService.Context;
using KNQASelfService.Interfaces.UserManagement;
using KNQASelfService.Models;

namespace KNQASelfService.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly AppDbContext _context;

        public RoleRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Role>> GetAllAsync()
        {
            return await _context.Roles.OrderBy(r => r.RoleName).ToListAsync();
        }

        public async Task<Role> GetByIdAsync(int id)
        {
            return await _context.Roles.FindAsync(id);
        }
    }
}