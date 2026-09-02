using System.Threading.Tasks;
using NotesApp.Api.Models;

namespace NotesApp.Api.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
    }
}
