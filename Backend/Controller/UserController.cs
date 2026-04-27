using Backend.Domain.Entities;
using Backend.DTOs;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRegisterDTO request)
    {
        string name = request.Name;
        string username = request.Username;
        string password = request.Password;

        if (
            string.IsNullOrEmpty(name)
            || string.IsNullOrEmpty(username)
            || string.IsNullOrEmpty(password)
        )
        {
            return BadRequest("Name, username, and password are required.");
        }
    }
}
