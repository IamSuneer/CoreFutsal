using CoreFutsal.Models;
using CoreFutsal.Models.ViewModels;
using CoreFutsal.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;

namespace CoreFutsal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService userService;

        public UsersController(IUserService userService)
        {
            this.userService = userService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await Task.FromResult(this.userService.GetAllUsers());
        }

        [HttpPost]
        public async Task<ActionResult<UserRegisterViewModel>> PostUser(UserRegisterViewModel model)
        {
            this.userService.AddUser(model);
            return await Task.FromResult(model);
        }
    }
}
