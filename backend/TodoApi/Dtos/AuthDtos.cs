namespace TodoApi.Dtos;

public record LoginRequest(string Username, string Password);
public record LoginResponse(string Token, string Username, int UserId);
public record RegisterRequest(string Username, string Password);
