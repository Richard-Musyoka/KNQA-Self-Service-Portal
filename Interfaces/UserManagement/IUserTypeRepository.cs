using KNQASelfService.Context;
using KNQASelfService.Models;
using Microsoft.EntityFrameworkCore;
namespace KNQASelfService.Interfaces.UserManagement
{
    public interface IUserTypeRepository
    {
        Task<List<UserTypes>> GetUserTypesAsync();
    }
    public class UserTypesRepo : IUserTypeRepository
    {
        private readonly AppDbContext _db;

        public UserTypesRepo(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<UserTypes>> GetUserTypesAsync()
        {
            return await _db.UserTypes
                .Select(ut => new UserTypes
                {
                    id = ut.id,
                    Description = ut.Description
                })
                .ToListAsync();
        }

    }
}
