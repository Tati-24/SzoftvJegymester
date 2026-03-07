using System.ComponentModel.DataAnnotations;

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Required, MaxLength(150)] string Name,
    [Required][Phone] string PhoneNumber);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);
public record AuthResponse(string Token, DateTime ExpiresAt);
