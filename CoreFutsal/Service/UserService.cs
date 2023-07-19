using CoreFutsal.DAL;
using CoreFutsal.Models;
using CoreFutsal.Models.ViewModels;
using Microsoft.AspNetCore.Identity;

namespace CoreFutsal.Service
{
    public class UserService : IUserService
    {
        private readonly FutsalContext context;
        private readonly IPlayerService player;

        public UserService(FutsalContext context, IPlayerService player)
        {
            this.context = context;
            this.player = player;
        }

        public List<User> GetAllUsers()
        {
            try
            {
                return this.context.Users.ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void AddUser(UserRegisterViewModel model)
        {
            try
            {
                var guid = Guid.NewGuid();
                
                User user = new User()
                {
                    UserId = guid,
                    Email = model.Email,
                    NormalizedEmail = model.Email.ToUpper(),
                    UserName = model.UserName,
                    NormalizedUserName = model.UserName.ToUpper(),
                    PhoneNumber = model.MobileNumber
                };
                PasswordHasher<User> hasher = new PasswordHasher<User>();
                user.PasswordHash = hasher.HashPassword(user, model.Password);

                this.context.Users.Add(user);
                this.player.AddPlayer(model, guid);
                this.context.SaveChanges();
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
