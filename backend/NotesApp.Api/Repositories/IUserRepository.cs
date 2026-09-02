using System;
using System.Threading.Tasks;
using NotesApp.Api.Models;

namespace NotesApp.Api.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdAsync(Guid id);
        Task<User> CreateAsync(string email, string displayName, string passwordHash);
        Task<bool> EmailExistsAsync(string email);
    }
}
