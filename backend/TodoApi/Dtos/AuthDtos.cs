namespace TodoApi.Dtos;

public record LoginRequest(string Username, string Password);
public record LoginResponse(string Token, string Username, string FullName, int UserId);
public record RegisterRequest(string Username, string FullName, string Password);
