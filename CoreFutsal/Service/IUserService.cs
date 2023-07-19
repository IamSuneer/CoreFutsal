using CoreFutsal.Models;
using CoreFutsal.Models.ViewModels;

namespace CoreFutsal.Service
{
    public interface IUserService
    {
        List<User> GetAllUsers();
        void AddUser(UserRegisterViewModel model);
    }
}
