using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AspNetServer.Controllers
{

    [ApiController]
    [Route("screenings")]
    public class ScreeningsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public ScreeningsController(ApplicationDbContext db)
        {
            _db = db;
        }

        /// <summary>Create a new screening; admin only</summary>
        [Authorize(Roles = "ADMIN")]
        [HttpPost("admin/upload")]

        public async Task<IActionResult> CreateScreening([FromBody] ScreeningCreateRequest dto)
        {
            var film = await _db.Films.FirstOrDefaultAsync(f => f.Id == dto.FilmId);
            if (film == null)
                return NotFound("Film not found.");

            var movieHall = await _db.MovieHalls.FirstOrDefaultAsync(h => h.Id == dto.MovieHallId);
            if (movieHall == null)
                return NotFound("Movie hall not found.");

            if (await _db.Screenings.AnyAsync(s => s.FilmId == dto.FilmId && s.MovieHallId == dto.MovieHallId && s.StartTime == dto.StartTime))
                return Conflict("Screening already exists with these parameters.");

            var screening = new Screenings
            {
                FilmId = dto.FilmId,
                MovieHallId = dto.MovieHallId,
                StartTime = dto.StartTime,
                BasePrice = dto.BasePrice
            };

            _db.Screenings.Add(screening);
            await _db.SaveChangesAsync();
            return Ok(ToResponse(screening, film, movieHall));
        }


        /// <summary>
        /// Update screening metadata by id; admin only
        /// </summary>        
        [Authorize(Roles = "ADMIN")]
        [HttpPut("admin/{id:guid}")]
        public async Task<IActionResult> UpdateScreening(Guid id, [FromBody] ScreeningUpdateRequest dto)
        {
            var screening = await _db.Screenings
                .Include(s => s.Film)
                .Include(s => s.MovieHall)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (screening == null)
                return NotFound("Screening not found.");
            if (screening.FilmId != dto.FilmId && dto.FilmId != null)
            {
                var film = await _db.Films.FirstOrDefaultAsync(f => f.Id == dto.FilmId);
                if (film == null)
                    return NotFound("Film not found.");

                screening.FilmId = dto.FilmId.Value;
                screening.Film = film;
            }
            if (screening.MovieHallId != dto.MovieHallId && dto.MovieHallId != null)
            {
                var movieHall = await _db.MovieHalls.FirstOrDefaultAsync(h => h.Id == dto.MovieHallId);
                if (movieHall == null)
                    return NotFound("Movie hall not found.");

                screening.MovieHallId = dto.MovieHallId.Value;
                screening.MovieHall = movieHall;
            }
            if (screening.StartTime != dto.StartTime && dto.StartTime != null) screening.StartTime = (DateTime)dto.StartTime;
            if (screening.BasePrice != dto.BasePrice && dto.BasePrice != null) screening.BasePrice = (decimal)dto.BasePrice;
            if (screening.IsCancelled != dto.IsCancelled && dto.IsCancelled != null) screening.IsCancelled = (bool)dto.IsCancelled;

            await _db.SaveChangesAsync();
            return Ok(ToResponse(screening));
        }

        /// <summary>
        /// Update screening metadata by id; admin only
        /// </summary>     
        [Authorize(Roles = "ADMIN")]
        [HttpDelete("admin/{id:guid}")]
        public async Task<IActionResult> DeleteScreening(Guid id)
        {
            var screening = await _db.Screenings
                .Include(s => s.Film)
                .Include(s => s.MovieHall)
                .Include(s => s.Tickets)
                .FirstOrDefaultAsync(s => s.Id == id);
            if(screening == null) return NotFound("Screening not found.");

            var response = new ScreeningDeleteResponse(
                "Vetítés törölve lett.",
                screening.FilmId,
                screening.Film.Title,
                screening.MovieHallId,
                screening.MovieHall.HallName);

            _db.Screenings.Remove(screening);
            await _db.SaveChangesAsync();
            return Ok(response);
        }

        /// <summary>
        /// Get list of active screenings
        /// </summary>     
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ListScreenings()
        {
            var screenings = await _db.Screenings
                .Where(s=>!s.IsCancelled)
                .Include(s => s.Film)
                .Include(s => s.MovieHall)
                .AsNoTracking()
                .ToListAsync();

            return Ok(screenings.Select(ToResponse));
        }

        /// <summary>
        /// Get a screening based on ID
        /// </summary>     
        [AllowAnonymous]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetScreening(Guid id)
        {
            var screening = await _db.Screenings                
                .Include(s => s.Film)
                .Include(s => s.MovieHall)
                .AsNoTracking()
                .FirstOrDefaultAsync(s=>s.Id == id && !s.IsCancelled);
            if (screening == null) return NotFound("Screening not found.");
            return Ok(ToResponse(screening));
        }

        private static ScreeningResponse ToResponse(Screenings screening)
        {
            return ToResponse(screening, screening.Film, screening.MovieHall);
        }

        private static ScreeningResponse ToResponse(Screenings screening, Film film, MovieHalls movieHall)
        {
            return new ScreeningResponse(
                screening.Id,
                screening.FilmId,
                film.Title,
                screening.MovieHallId,
                movieHall.HallName,
                screening.StartTime,
                screening.BasePrice,
                screening.IsCancelled);
        }
    }
}
