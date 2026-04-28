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
        
        [HttpPost("purchase")]

        public async Task<IActionResult> PurchaseTicket([FromBody] TicketPurchaseRequest dto)
        {
            if (await _db.Tickets.AnyAsync(t=>t.ScreeningId == dto.ScreeningId && t.SeatNumber == dto.SeatNumber))
                return Conflict("Ticket already purchased.");

            var ticket = new Tickets
            {
                ScreeningId = dto.ScreeningId,
                SeatNumber = dto.SeatNumber,
                TicketPrice = (decimal)dto.TicketPrice
            };
            if (User.Identity?.IsAuthenticated == true)
            {                
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (Guid.TryParse(userIdClaim, out Guid loggedInUserId))
                {
                    ticket.UserId = loggedInUserId;
                }
            }
            else
            {                
                if (string.IsNullOrEmpty(dto.GuestEmail) || string.IsNullOrEmpty(dto.GuestPhone))
                {
                    return BadRequest("For not registered users giving phone number and email is mandatory.");
                }

                ticket.Guest = new Guest
                {
                    Email = dto.GuestEmail,
                    Name = dto.GuestName,
                    PhoneNumber = dto.GuestPhone
                };
            }

            _db.Tickets.Add(ticket);
            await _db.SaveChangesAsync();
            return Ok(ticket);
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
                .ToListAsync();
                        
            return Ok(tickets);
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
            var tickets = await _db.Tickets
                .Where(t => t.UserId == userId)
                .Include(t => t.Screening)
                    .ThenInclude(s => s.Film)
                .ToListAsync();
            var ticket = await _db.Tickets
                .Where(t => t.UserId == userId)
                .Include(t => t.Screening)
                    .ThenInclude(s => s.Film)
                .FirstAsync();
            if (tickets.Contains(ticket))
            {
                if (ticket.Screening.StartTime - DateTime.Now < TimeSpan.FromHours(4))
                {
                    _db.Remove(ticket);
                    await _db.SaveChangesAsync();
                }

            }
            else
            {
                return Unauthorized("User does not own that ticket.");
            }

                return Ok("Ticket deleted.");
        }

        /// <summary>
        /// Purchase a ticket; cashiers only
        /// </summary>        
        [Authorize(Roles = "CASHIER")]
        [HttpPost("cashier/purchase")]
        public async Task<IActionResult> CashierPurchaseTickets( [FromBody] TicketPurchaseRequest dto)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid userId))
            {
                return Unauthorized("Invalid User identification.");
            }
            if (await _db.Tickets.AnyAsync(t => t.ScreeningId == dto.ScreeningId && t.SeatNumber == dto.SeatNumber))
                return Conflict("Ticket already purchased.");

            var ticket = new Tickets
            {
                ScreeningId = dto.ScreeningId,
                SeatNumber = dto.SeatNumber,
                TicketPrice = (decimal)dto.TicketPrice
            };
            
            if (string.IsNullOrEmpty(dto.GuestEmail) || string.IsNullOrEmpty(dto.GuestPhone))
            {
                return BadRequest("For not registered users giving phone number and email is mandatory.");
            }

            ticket.Guest = new Guest
            {
                Email = dto.GuestEmail,
                Name = dto.GuestName,
                PhoneNumber = dto.GuestPhone
            };            

            _db.Tickets.Add(ticket);
            await _db.SaveChangesAsync();
            return Ok(ticket);
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
               .Where(t => t.UserId == userId)
               .Include(t => t.Screening)
                   .ThenInclude(s => s.Film)
               .FirstAsync();
            if (ticket == null)
                return NotFound("Invalid ticket id");
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
               .Where(t => t.UserId == userId)
               .Include(t => t.Screening)
                   .ThenInclude(s => s.Film)
               .FirstAsync();
            if (ticket == null)
                return NotFound("Invalid ticket id");
            return Ok(ticket);
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

            return Ok(tickets);
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
                .GroupBy(t => t.Screening.Film.Title)
                .Select(g => new {
                    MovieTitle = g.Key,
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
}


}
