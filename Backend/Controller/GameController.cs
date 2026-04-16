using Backend.Domain.GameEntity;

namespace Backend.Controller.GameController;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
class GameController : ControllerBase
{
    private readonly Game _game;

    public GameController(Game game)
    {
        _game = game;
    }
}
