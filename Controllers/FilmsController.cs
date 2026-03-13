using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AspNetServer.Controllers;

[ApiController]
[Route("films")]
public class FilmsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public FilmsController(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>Create a new film record; admin only.</summary>
    [Authorize(Roles = "ADMIN")]
    [HttpPost("admin/upload")]
    public async Task<IActionResult> CreateFilm([FromBody] FilmCreateRequest dto)
    {
        var title = dto.Title.Trim();
        if (await _db.Films.AnyAsync(f => f.Title.ToLower() == title.ToLower()))
        {
            return Conflict("Film already exists with this title.");
        }

        var film = new Film
        {
            Title = title,
            Description = dto.Description,
            Length = dto.Length,
            AgeRating = dto.AgeRating,
            ReleaseDate = dto.ReleaseDate,
            Genre = dto.Genre,
            Director = dto.Director,
            IsActive = dto.IsActive
        };

        _db.Films.Add(film);
        await _db.SaveChangesAsync();
        return Ok(film);
    }

    /// <summary>Update mutable film metadata by id; admin only.</summary>
    [Authorize(Roles = "ADMIN")]
    [HttpPut("admin/{id:guid}")]
    public async Task<IActionResult> UpdateFilm(Guid id, [FromBody] FilmUpdateRequest dto)
    {
        var film = await _db.Films.FirstOrDefaultAsync(f => f.Id == id);
        if (film == null)
        {
            return NotFound("Film not found.");
        }

        if (!string.IsNullOrWhiteSpace(dto.Title))
        {
            var normalized = dto.Title.Trim();
            var exists = await _db.Films.AnyAsync(f => f.Id != id && f.Title.ToLower() == normalized.ToLower());
            if (exists) return Conflict("Another film already uses this title.");
            film.Title = normalized;
        }
        if (!string.IsNullOrWhiteSpace(dto.Description)) film.Description = dto.Description;
        if (dto.Length.HasValue) film.Length = dto.Length.Value;
        if (dto.AgeRating != null) film.AgeRating = dto.AgeRating;
        if (dto.ReleaseDate.HasValue) film.ReleaseDate = dto.ReleaseDate.Value;
        if (dto.Genre != null) film.Genre = dto.Genre;
        if (dto.Director != null) film.Director = dto.Director;
        if (dto.IsActive.HasValue) film.IsActive = dto.IsActive.Value;

        await _db.SaveChangesAsync();
        return Ok(film);
    }

    /// <summary>Delete film when no active screenings are attached; admin only.</summary>
    [Authorize(Roles = "ADMIN")]
    [HttpDelete("admin/{id:guid}")]
    public async Task<IActionResult> DeleteFilm(Guid id)
    {
        var film = await _db.Films.Include(f => f.Screenings).FirstOrDefaultAsync(f => f.Id == id);
        if (film == null) return NotFound("Film not found.");

        var hasActiveScreenings = film.Screenings.Any(s => !s.IsCancelled && s.StartTime > DateTime.UtcNow);
        if (hasActiveScreenings)
        {
            return Conflict("Film cannot be removed while it has active screenings.");
        }

        _db.Films.Remove(film);
        await _db.SaveChangesAsync();
        return Ok($"Film '{film.Title}' removed.");
    }

    /// <summary>Public list of active films.</summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> ListFilms()
    {
        var films = await _db.Films
            .Where(f => f.IsActive)
            .AsNoTracking()
            .ToListAsync();
        return Ok(films);
    }

    /// <summary>Public film details with non-cancelled screenings.</summary>
    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetFilm(Guid id)
    {
        var film = await _db.Films
            .Include(f => f.Screenings.Where(s => !s.IsCancelled))
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id);

        if (film == null) return NotFound("Film not found.");
        return Ok(film);
    }
}
