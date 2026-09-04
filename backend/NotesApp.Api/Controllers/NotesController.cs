using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NotesApp.Api.Models;
using NotesApp.Api.Repositories;

namespace NotesApp.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/notes")]
    public class NotesController : ControllerBase
    {
        private readonly INoteRepository _noteRepository;

        public NotesController(INoteRepository noteRepository)
        {
            _noteRepository = noteRepository;
        }

        private Guid CurrentUserId
        {
            get
            {
                var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub");

                if (subject is null || !Guid.TryParse(subject, out var userId))
                    throw new UnauthorizedAccessException("Token is missing a valid subject claim.");

                return userId;
            }
        }

        [HttpGet]
        [ProducesResponseType(typeof(NoteDto[]), StatusCodes.Status200OK)]
        public async Task<ActionResult<NoteDto[]>> GetAll()
        {
            var notes = await _noteRepository.GetAllForUserAsync(CurrentUserId);
            return Ok(notes.Select(NoteDto.FromEntity).ToArray());
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(NoteDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<NoteDto>> GetById(Guid id)
        {
            var note = await _noteRepository.GetByIdForUserAsync(id, CurrentUserId);
            if (note is null)
                return NotFound();

            return Ok(NoteDto.FromEntity(note));
        }

        [HttpPost]
        [ProducesResponseType(typeof(NoteDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<NoteDto>> Create([FromBody] CreateNoteRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                return BadRequest(new { message = "Title is required." });

            var note = await _noteRepository.CreateAsync(
                CurrentUserId, request.Title.Trim(), request.Content ?? string.Empty, request.IsPinned);

            var dto = NoteDto.FromEntity(note);
            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(NoteDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<NoteDto>> Update(Guid id, [FromBody] UpdateNoteRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                return BadRequest(new { message = "Title is required." });

            var note = await _noteRepository.UpdateAsync(
                id, CurrentUserId, request.Title.Trim(), request.Content ?? string.Empty, request.IsPinned);

            if (note is null)
                return NotFound();

            return Ok(NoteDto.FromEntity(note));
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var deleted = await _noteRepository.DeleteAsync(id, CurrentUserId);
            if (!deleted)
                return NotFound();

            return NoContent();
        }
    }
}
