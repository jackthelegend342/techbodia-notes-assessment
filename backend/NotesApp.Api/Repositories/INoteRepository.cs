using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NotesApp.Api.Models;

namespace NotesApp.Api.Repositories
{
    /// <summary>
    /// Every method requires the caller's userId and enforces row-level
    /// ownership at the SQL level (WHERE user_id = @UserId).
    /// </summary>
    public interface INoteRepository
    {
        Task<IEnumerable<Note>> GetAllForUserAsync(Guid userId);
        Task<Note?> GetByIdForUserAsync(Guid noteId, Guid userId);
        Task<Note> CreateAsync(Guid userId, string title, string content, bool isPinned);
        Task<Note?> UpdateAsync(Guid noteId, Guid userId, string title, string content, bool isPinned);
        Task<bool> DeleteAsync(Guid noteId, Guid userId);
    }
}
