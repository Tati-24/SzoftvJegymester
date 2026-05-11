using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Sockets;
using System.Security.Claims;

namespace AspNetServer.Controllers
{
    
    [ApiController]
    [Route("tickets")]
    public class TicketsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public TicketsController(ApplicationDbContext db)
        {
            _db = db;
        }

        /// <summary>Purchase a ticket</summary>
        [AllowAnonymous]
        [HttpPost("purchase")]
        public async Task<IActionResult> PurchaseTicket([FromBody] TicketPurchaseRequest dto)
        {
            return await PurchaseTicketInternal(dto, "Screening not found.", false);
        }


        /// <summary>Purchase a ticket</summary>
        [HttpGet("my-tickets")]
        [Authorize]
        public async Task<IActionResult> GetMyTickets()
        {            
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid userId))
            {
                return Unauthorized("Invalid User identification.");
            }
            var tickets = await _db.Tickets
                .Where(t => t.UserId == userId)
                .Include(t => t.Screening)
                    .ThenInclude(s => s.Film)
                .Include(t => t.Screening)
                    .ThenInclude(s => s.MovieHall)
                .Include(t => t.User)
                .Include(t => t.Guest)
                .ToListAsync();
                        
            return Ok(tickets.Select(ToResponse));
        }

        [HttpDelete("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> DeleteTicket(Guid id)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid userId))
            {
                return Unauthorized("Invalid User identification.");
            }
            var ticket = await _db.Tickets
                .Include(t => t.Screening)
                    .ThenInclude(s => s.Film)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
            {
                return NotFound("Invalid ticket id.");
            }

            if (ticket.UserId != userId)
            {
                return Unauthorized("User does not own that ticket.");
            }

            if (ticket.Screening.StartTime - DateTime.UtcNow < TimeSpan.FromHours(4))
            {
                return Conflict("Ticket cannot be deleted within 4 hours of the screening.");
            }

            _db.Remove(ticket);
            await _db.SaveChangesAsync();

            return Ok("Ticket deleted.");
        }

        /// <summary>
        /// Purchase a ticket; cashiers only
        /// </summary>        
        [Authorize(Roles = "CASHIER")]
        [HttpPost("cashier/purchase")]
        public async Task<IActionResult> CashierPurchaseTickets( [FromBody] TicketPurchaseRequest dto)
        {
            return await PurchaseTicketInternal(dto, "The requested screening does not exist.", true);
        }
          
                                        
        /// <summary>
        /// Validate a ticket; cashiers only
        /// </summary>        
        [Authorize(Roles = "CASHIER")]
        [HttpPatch("{id:guid}/validate")]
        public async Task<IActionResult> ValidateTicket(Guid id)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid userId))
            {
                return Unauthorized("Invalid User identification.");
            }
            var ticket = await _db.Tickets
               .Include(t => t.Screening)
                   .ThenInclude(s => s.Film)
               .FirstOrDefaultAsync(t => t.Id == id);
            if (ticket == null)
                return NotFound("Invalid ticket id.");
            ticket.IsValidated = true;
            ticket.ValidatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok("Ticket validated.");
        }
        /// <summary>
        /// Get ticket; cashier/admin only
        /// </summary>        
        [Authorize(Roles = "ADMIN, CASHIER")]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetTicketCashier(Guid id)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid userId))
            {
                return Unauthorized("Invalid User identification.");
            }
            var ticket = await _db.Tickets
               .Include(t => t.Screening)
                   .ThenInclude(s => s.Film)
               .Include(t => t.Screening)
                   .ThenInclude(s => s.MovieHall)
               .Include(t => t.User)
               .Include(t => t.Guest)
               .FirstOrDefaultAsync(t => t.Id == id);
            if (ticket == null)
                return NotFound("Invalid ticket id.");
            return Ok(ToResponse(ticket));
        }
        /// <summary>
        /// Get all tickets, potentially querried; admin only
        /// </summary>        
        [Authorize(Roles = "ADMIN")]
        [HttpGet]
        public async Task<IActionResult> GetAllTickets([FromQuery] Guid? filmId = null,
        [FromQuery] Guid? screeningId = null,
        [FromQuery] DateTime? date = null)
        {
            var query = _db.Tickets
            .Include(t => t.Screening)
                .ThenInclude(s => s.Film)
            .Include(t => t.Screening)
                .ThenInclude(s => s.MovieHall)
            .Include(t => t.User)
            .Include(t => t.Guest)
            .AsQueryable();
            
            if (filmId.HasValue)
                query = query.Where(t => t.Screening.FilmId == filmId.Value);
            
            if (screeningId.HasValue)
                query = query.Where(t => t.ScreeningId == screeningId.Value);
            
            if (date.HasValue)
                query = query.Where(t => t.Screening.StartTime.Date == date.Value.Date);

            var tickets = await query.OrderByDescending(t => t.PurchasedAt).ToListAsync();

            return Ok(tickets.Select(ToResponse));
        }

        /// <summary>
        /// Get statistics; admin only
        /// </summary>        
        [Authorize(Roles = "ADMIN")]
        [HttpGet("stats")]
        public async Task<IActionResult> GetStatictics()
        {
            var allTickets = await _db.Tickets
           .Include(t => t.Screening)
               .ThenInclude(s => s.Film)
           .Where(t => !t.IsCancelled) 
           .ToListAsync();

            var totalRevenue = allTickets.Sum(t => t.TicketPrice);
            var totalTicketsSold = allTickets.Count();        
            var topMovies = allTickets
                .GroupBy(t => new { t.Screening.FilmId, t.Screening.Film.Title })
                .Select(g => new {
                    FilmId = g.Key.FilmId,
                    MovieTitle = g.Key.Title,
                    TicketsSold = g.Count(),
                    Revenue = g.Sum(t => t.TicketPrice)
                })
                .OrderByDescending(x => x.TicketsSold)
                .Take(5)
                .ToList();

            return Ok(new
            {
                TotalRevenue = totalRevenue,
                TotalTicketsSold = totalTicketsSold,
                TopMovies = topMovies,
                GeneratedAt = DateTime.UtcNow
            });
        }

        private async Task<IActionResult> PurchaseTicketInternal(TicketPurchaseRequest dto, string screeningNotFoundMessage, bool validateImmediately)
        {
            var screening = await _db.Screenings
                .Include(s => s.MovieHall)
                .FirstOrDefaultAsync(s => s.Id == dto.ScreeningId);

            if (screening == null)
                return NotFound(screeningNotFoundMessage);

            if (screening.IsCancelled)
                return BadRequest("This screening has been cancelled.");

            var soldTickets = await _db.Tickets
                .CountAsync(t => t.ScreeningId == dto.ScreeningId && !t.IsCancelled);

            if (soldTickets >= screening.MovieHall.SeatCount)
                return Conflict("No more tickets can be purchased because the movie hall is full.");

            if (await _db.Tickets.AnyAsync(t => t.ScreeningId == dto.ScreeningId && t.SeatNumber == dto.SeatNumber && !t.IsCancelled))
                return Conflict("Ticket already purchased.");

            var ticket = new Tickets
            {
                ScreeningId = dto.ScreeningId,
                SeatNumber = dto.SeatNumber,
                TicketPrice = screening.BasePrice,
                IsValidated = validateImmediately,
                ValidatedAt = validateImmediately ? DateTime.UtcNow : null
            };

            switch (dto.BuyerType)
            {
                case TicketBuyerType.RegisteredUser:
                    if (dto.UserId == null)
                        return BadRequest("UserId is required for registered user purchases.");

                    var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == dto.UserId);
                    if (user == null)
                        return NotFound("User not found.");

                    ticket.UserId = user.Id;
                    ticket.User = user;
                    break;

                case TicketBuyerType.Guest:
                    if (string.IsNullOrWhiteSpace(dto.GuestName) || string.IsNullOrWhiteSpace(dto.GuestEmail) || string.IsNullOrWhiteSpace(dto.GuestPhone))
                    {
                        return BadRequest("For guest purchases name, phone number and email are mandatory.");
                    }

                    var guest = new Guest
                    {
                        Email = dto.GuestEmail,
                        Name = dto.GuestName,
                        PhoneNumber = dto.GuestPhone
                    };
                    ticket.Guest = guest;
                    guest.Tickets.Add(ticket);
                    break;

                default:
                    return BadRequest("Invalid buyer type.");
            }

            _db.Tickets.Add(ticket);
            await _db.SaveChangesAsync();

            var response = new TicketPurchaseResponse(
                 ticket.ScreeningId,
                 ticket.SeatNumber,
                 screening.BasePrice,
                 DateTime.UtcNow,
                 ticket.UserId,
                 dto.BuyerType == TicketBuyerType.Guest ? dto.GuestName : null,
                 dto.BuyerType == TicketBuyerType.Guest ? dto.GuestEmail : null,
                 dto.BuyerType == TicketBuyerType.Guest ? dto.GuestPhone : null
             );
            return Ok(response);
        }

        private static TicketResponse ToResponse(Tickets ticket)
        {
            return new TicketResponse(
                ticket.Id,
                ticket.ScreeningId,
                ticket.Screening.FilmId,
                ticket.Screening.Film.Title,
                ticket.Screening.MovieHallId,
                ticket.Screening.MovieHall.HallName,
                ticket.Screening.StartTime,
                ticket.SeatNumber,
                ticket.TicketPrice,
                ticket.PurchasedAt,
                ticket.UserId.HasValue ? TicketBuyerType.RegisteredUser : TicketBuyerType.Guest,
                ticket.UserId,
                ticket.User?.Name,
                ticket.User?.Email,
                ticket.GuestId,
                ticket.Guest?.Name,
                ticket.Guest?.Email,
                ticket.Guest?.PhoneNumber,
                ticket.IsValidated,
                ticket.ValidatedAt,
                ticket.IsCancelled,
                ticket.CancelledAt);
        }
}


}
