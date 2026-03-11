namespace AspNetServer.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;

    [ApiController]
    [Route("data")]

    public class DataController : ControllerBase
    {
        private readonly ApplicationDbContext _db;        

        public DataController(ApplicationDbContext db)
        {
            _db = db;           
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPost("/film/upload")]
        public async Task<IActionResult> UploadMovie(string title, string description, int length, string AgeRating, DateTime releaseDate, string genre, string director, bool isActive, ICollection<Screenings> screenings)
        {            
                if (await _db.Films.AnyAsync(f => f.Title == title))
                {
                    return Conflict("Film already uploaded!");
                }
                var film = new Film
                {
                    Title = title,
                    Description = description,
                    Lengt = length,
                    ReleaseDate = releaseDate,
                    Genre = genre,
                    Director = director,
                    IsActive = isActive,
                    Screenings = screenings
                };

                _db.Films.Add(film);
                await _db.SaveChangesAsync();
                return Ok(film);           
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPost("/film/remove/{id}")]
        public async Task<IActionResult> RemoveMovie(Guid id)
        {
            
            if (!(await _db.Films.AnyAsync(f => f.Id == id)))
            {
                return Conflict("Film already removed!");
            }

            var film = await _db.Films.Where(f => f.Id == id).FirstOrDefaultAsync();
            _db.Films.Remove(film);
            await _db.SaveChangesAsync();
            return Ok($"{film.Title} succesfully removed");
        }

        [Authorize(Roles = "ADMIN")]
        [Authorize(Roles = "CASHIER")]
        [HttpPost("/film/update/{id}")]
        public async Task<IActionResult> UpdateMovie(Guid id, Film newData)
        {

            if (!(await _db.Films.AnyAsync(f => f.Id == id)))
            {
                return Conflict("Film not found!");
            }

            var film = await _db.Films.Where(f => f.Id == id).FirstOrDefaultAsync();
            
            if (film.IsActive != newData.IsActive)
                film.IsActive = newData.IsActive;
            
            if(film.Screenings != newData.Screenings && newData.Screenings != null)
                film.Screenings = newData.Screenings;

            _db.Films.Update(film);
            await _db.SaveChangesAsync();
            return Ok($"{film.Title} succesfully removed");
        }

        [HttpPost("/film/view")]
        public async Task<IActionResult> ViewFilms()
        {
            var films =  await _db.Films.Where(f=>f.IsActive).ToListAsync();
            return Ok(films);
        }

        [HttpPost("/film/view/{id}")]
        public async Task<ActionResult<Film>> ViewFilm(Guid id)
        {
            var film = await _db.Films.Where(f => f.Id == id).FirstOrDefaultAsync();
            if (film == null)
                return Conflict("Film not found!");
            return Ok(film);
        }



        [Authorize(Roles = "ADMIN")]
        [HttpPost("/screening/upload")]
        public async Task<IActionResult> UploadScreening(Screenings screening)
        {
            if (await _db.Screenings.AnyAsync(s => s.Id == screening.Id))
            {
                return Conflict("Screening already registered!");
            }            

            _db.Screenings.Add(screening);
            await _db.SaveChangesAsync();
            return Ok(screening);
        }

        [Authorize(Roles = "ADMIN")]
        [Authorize(Roles = "CASHIER")]
        [HttpPost("/screening/update/{id}")]
        public async Task<IActionResult> UpdateScreening(Guid id, Screenings newData)
        {
            var screening = await _db.Screenings.Where(s => s.Id == id).FirstOrDefaultAsync();
            if (screening == null)
                return Conflict("Screening not found!");
            
            if(screening.FilmId != newData.FilmId && newData.FilmId!= null) { 
                screening.FilmId = newData.FilmId;
                screening.Film = newData.Film ?? screening.Film;
            }
            
            if(screening.MovieHallId != newData.MovieHallId && newData.MovieHallId != null)
            {
                screening.MovieHallId = newData.MovieHallId;
                screening.MovieHall = newData.MovieHall ?? screening.MovieHall;
            }
            
            if(screening.StartTime != newData.StartTime)
                screening.StartTime = newData.StartTime;

            if(screening.BasePrice != newData.BasePrice)
                screening.BasePrice = newData.BasePrice;

            if(screening.IsCancelled != newData.IsCancelled)
                screening.IsCancelled = newData.IsCancelled;

            screening.Tickets = newData.Tickets ?? screening.Tickets;

            _db.Screenings.Update(screening);
            await _db.SaveChangesAsync();
            return Ok(screening);
        }

        [HttpPost("/screening/view")]
        public async Task<IActionResult> ViewScreenings()
        {
            var screenings = await _db.Screenings.Where(s => s.IsCancelled == false).ToListAsync();
            return Ok(screenings);
        }

        [HttpPost("/screening/view/{id}")]
        public async Task<IActionResult> ViewScreening(Guid id)
        {
            var screening = await _db.Screenings.Where(s => s.Id == id).FirstOrDefaultAsync();
            if (screening == null)
                return Conflict("Screening not found!");
            return Ok(screening);
        }
    }


}
