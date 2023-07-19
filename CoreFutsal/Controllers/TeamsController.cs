using CoreFutsal.Models;
using CoreFutsal.Models.ViewModels;
using CoreFutsal.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CoreFutsal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TeamsController : ControllerBase
    {
        private readonly ITeamService teamService;

        public TeamsController(ITeamService teamService)
        {
            this.teamService = teamService;
        }
        // GET: api/<TeamsController>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Team>>> Get()
        {
            return await Task.FromResult(this.teamService.GetAllTeams());
        }

        // GET api/<TeamsController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<TeamsController>
        [HttpPost]
        public async Task<ActionResult<TeamRegisterViewModel>> Post(TeamRegisterViewModel model)
        {
            this.teamService.AddTeam(model, User.Claims.ToArray()[3].Value);
            return await Task.FromResult(model);
        }

        // PUT api/<TeamsController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<TeamsController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
