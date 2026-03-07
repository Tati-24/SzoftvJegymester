using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("auth")]

public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IPasswordHasher<User> _hasher;
    private readonly TokenService _tokens;

    public AuthController(ApplicationDbContext db, IPasswordHasher<User> hasher, TokenService tokens)
    {
        _db = db;
        _hasher = hasher;
        _tokens = tokens;
    }

    [HttpPost("/register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        if(await _db.Users.AnyAsync(u => u.Email == email))
        {
            return Conflict("Email already registered");
        }
        var user = new User
        {
            Email = email,
            Name = dto.Name,
            PhoneNumber = dto.PhoneNumber,
            Role = Roles.USER,
            PassWordHash = _hasher.HashPassword(null!, dto.Password)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return _tokens.CreateToken(user);
    }

    [HttpPost("/login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == email);
        if(user == null)
        {
            return Conflict("Email not registered or incorrect");
        }

        var result = _hasher.VerifyHashedPassword(user, user.PassWordHash, dto.Password);
        if(result == PasswordVerificationResult.Failed)
        {
            return Unauthorized();
        }

        return _tokens.CreateToken(user);
        }  
    }
