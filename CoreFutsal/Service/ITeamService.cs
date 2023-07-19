using CoreFutsal.Models;
using CoreFutsal.Models.ViewModels;

namespace CoreFutsal.Service
{
    public interface ITeamService
    {
        List<Team> GetAllTeams();
        void AddTeam(TeamRegisterViewModel model, string user);
    }
}
