using CoreFutsal.DAL;
using CoreFutsal.Models;
using CoreFutsal.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CoreFutsal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TokenController : ControllerBase
    {
        public IConfiguration configuration;
        private readonly FutsalContext context;

        public TokenController(IConfiguration config, FutsalContext context)
        {
            this.configuration = config;
            this.context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Post(UserLoginViewModel userData)
        {
            if (userData != null && userData.UserName != null && userData.Password != null)
            {


                var user = await GetUser(userData.UserName, userData.Password);

                if (user != null)
                {
                    var claims = new[] {
                        new Claim(JwtRegisteredClaimNames.Sub, this.configuration["Jwt:Subject"]),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                        new Claim(JwtRegisteredClaimNames.Iat, DateTime.UtcNow.ToString()),
                        new Claim("UserId", user.UserId.ToString()),
                        new Claim("UserName", user.UserName),
                        new Claim("Email", user.Email)
                    };

                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(this.configuration["Jwt:Key"]));
                    var signIn = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                    var token = new JwtSecurityToken(
                        this.configuration["Jwt:Issuer"],
                        this.configuration["Jwt:Audience"],
                        claims,
                        expires: DateTime.UtcNow.AddMinutes(10),
                        signingCredentials: signIn);

                    return Ok(new JwtSecurityTokenHandler().WriteToken(token));
                }
                else
                {
                    return BadRequest("Invalid credentials");
                }
            }
            else
            {
                return BadRequest();
            }
        }

        private async Task<User> GetUser(string userName, string password)
        {
            var user = await this.context.Users.FirstOrDefaultAsync(u => u.UserName == userName || u.Email == userName);
            if (user != null)
            {
                PasswordHasher<User> hasher = new PasswordHasher<User>();
                var checkPassword = hasher.VerifyHashedPassword(user, user.PasswordHash, password);
                if (checkPassword.ToString() == "Success")
                {
                    return await Task.FromResult(user);
                }
            }

            return null;
        }
    }
}
