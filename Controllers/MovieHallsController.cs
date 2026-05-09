using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AspNetServer.Controllers;

[ApiController]
[Route("movie-halls")]
public class MovieHallsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public MovieHallsController(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>Create a movie hall; admin only.</summary>
    [Authorize(Roles = "ADMIN")]
    [HttpPost("admin/upload")]
    public async Task<IActionResult> CreateMovieHall([FromBody] MovieHallCreateRequest dto)
    {
        var hallName = dto.HallName.Trim();
        if (await _db.MovieHalls.AnyAsync(h => h.HallName.ToLower() == hallName.ToLower()))
        {
            return Conflict("Movie hall already exists with this name.");
        }

        var hall = new MovieHalls
        {
            HallName = hallName,
            SeatCount = dto.SeatCount,
            IsOccupied = dto.IsOccupied
        };

        _db.MovieHalls.Add(hall);
        await _db.SaveChangesAsync();
        return Ok(hall);
    }

    /// <summary>Update movie hall metadata by id; admin only.</summary>
    [Authorize(Roles = "ADMIN")]
    [HttpPut("admin/{id:guid}")]
    public async Task<IActionResult> UpdateMovieHall(Guid id, [FromBody] MovieHallUpdateRequest dto)
    {
        var hall = await _db.MovieHalls.FirstOrDefaultAsync(h => h.Id == id);
        if (hall == null)
        {
            return NotFound("Movie hall not found.");
        }

        if (!string.IsNullOrWhiteSpace(dto.HallName))
        {
            var hallName = dto.HallName.Trim();
            var exists = await _db.MovieHalls.AnyAsync(h => h.Id != id && h.HallName.ToLower() == hallName.ToLower());
            if (exists)
            {
                return Conflict("Another movie hall already uses this name.");
            }

            hall.HallName = hallName;
        }

        if (dto.SeatCount.HasValue) hall.SeatCount = dto.SeatCount.Value;
        if (dto.IsOccupied.HasValue) hall.IsOccupied = dto.IsOccupied.Value;

        await _db.SaveChangesAsync();
        return Ok(hall);
    }

    /// <summary>Delete movie hall when no screenings are attached; admin only.</summary>
    [Authorize(Roles = "ADMIN")]
    [HttpDelete("admin/{id:guid}")]
    public async Task<IActionResult> DeleteMovieHall(Guid id)
    {
        var hall = await _db.MovieHalls
            .Include(h => h.Screenings)
            .FirstOrDefaultAsync(h => h.Id == id);
        if (hall == null)
        {
            return NotFound("Movie hall not found.");
        }

        if (hall.Screenings.Any())
        {
            return Conflict("Movie hall cannot be removed while screenings are attached.");
        }

        _db.MovieHalls.Remove(hall);
        await _db.SaveChangesAsync();
        return Ok($"Movie hall '{hall.HallName}' removed.");
    }

    /// <summary>List movie halls so clients can use their ids.</summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> ListMovieHalls()
    {
        var halls = await _db.MovieHalls
            .AsNoTracking()
            .ToListAsync();

        return Ok(halls);
    }

    /// <summary>Get movie hall details by id.</summary>
    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetMovieHall(Guid id)
    {
        var hall = await _db.MovieHalls
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == id);

        if (hall == null)
        {
            return NotFound("Movie hall not found.");
        }

        return Ok(hall);
    }
}
