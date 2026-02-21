public record RegisterRequest(string Email, string Password, string Name, string? PhoneNumber);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, DateTime ExpiresAt);
