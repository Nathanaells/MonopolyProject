
namespace Backend.Domain.DTOs;

public class GameResultDTO<T>
{
    public bool IsSuccess { get; set; }
    public string? Error { get; set; }
    public T? Data { get; set; }

    public static GameResultDTO<T> Success(T data) => new GameResultDTO<T>
    {
        IsSuccess = true,
        Data = data
    };

    public static GameResultDTO<T> Failure(string error) => new GameResultDTO<T>
    {
        IsSuccess = false,
        Error = error
    };
}