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
        if(await _db.Users.AnyAsync(u => u.Email == dto.Email))
        {
            return Conflict("Email already registered");
        }

        var user = new User
        {
            Email = dto.Email,
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
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == dto.Email);
        if(user == null)
        {
            return Unauthorized();
        }

        var result = _hasher.VerifyHashedPassword(user, user.PassWordHash, dto.Password);
        if(result == PasswordVerificationResult.Failed)
        {
            return Unauthorized();
        }

        return _tokens.CreateToken(user);
        }  
    }