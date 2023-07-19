using CoreFutsal.Models;
using CoreFutsal.Models.DTOs;
using CoreFutsal.Models.ViewModels;

namespace CoreFutsal.Service
{
    public interface IPlayerService
    {
        public List<Player> GetAllPlayers();
        void AddPlayer(UserRegisterViewModel model, Guid id);
        void UpdatePlayer(PlayerEditDTO model, Guid id);

        bool CheckPlayer(Guid id);
    }
}
