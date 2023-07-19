using CoreFutsal.Models;
using CoreFutsal.Models.DTOs;
using CoreFutsal.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoreFutsal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PlayersController : ControllerBase
    {
        private readonly IPlayerService playerService;

        public PlayersController(IPlayerService playerService)
        {
            this.playerService = playerService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Player>>> Get()
        {
            return await Task.FromResult(this.playerService.GetAllPlayers());
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<PlayerEditDTO>> Put(string id, PlayerEditDTO model)
        {
            var playerId = User.Claims.ToList()[3].Value;
            if (id != playerId)
            {
                return BadRequest();
            }
            try
            {
                this.playerService.UpdatePlayer(model, Guid.Parse(playerId));
                return await Task.FromResult(model);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PlayerExists(Guid.Parse(playerId)))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        private bool PlayerExists(Guid id)
        {
            return this.playerService.CheckPlayer(id);
        }
    }
}
