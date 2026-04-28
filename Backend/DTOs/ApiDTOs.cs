namespace Backend.DTOs;

public record UserRegisterDTO(string Name, string Username, string Password);

public record UserLoginDTO(string Username, string Password);
