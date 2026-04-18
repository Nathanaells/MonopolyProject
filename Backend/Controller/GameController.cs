using Backend.Domain.GameEntity;

namespace Backend.Controller.GameController;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
class GameController : ControllerBase
{
    private readonly Game _game;

    public GameController()
    {
        
    }

    [HttpGet("startgame")]
    public void StartGame(){

    }

}
