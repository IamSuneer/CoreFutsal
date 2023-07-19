using CoreFutsal.DAL;
using CoreFutsal.Models;
using CoreFutsal.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Numerics;

namespace CoreFutsal.Service
{
    public class TeamService : ITeamService
    {
        private readonly FutsalContext context;

        public TeamService(FutsalContext context)
        {
            this.context = context;
        }

        public void AddTeam(TeamRegisterViewModel model, string playerId)
        {
            try
            {
                var guid = Guid.NewGuid();
                Team team = new Team()
                {
                    TeamId = guid,
                    TeamName = model.TeamName,
                    TeamDescription = model.TeamDescription,
                    TeamAddress = model.TeamAddress
                };
                team.Players.Add(this.context.Players.Where(p => p.PlayerId.ToString() == playerId).FirstOrDefault());
                var checkPlayer = this.context.TeamPlayers.Where(p => p.PlayerId == Guid.Parse(playerId)).FirstOrDefault();
                if (checkPlayer == null)
                {
                    TeamPlayer teamPlayer = new TeamPlayer()
                    {
                        TeamId = guid,
                        PlayerId = Guid.Parse(playerId)
                    };
                    this.context.Teams.Add(team);
                    this.context.TeamPlayers.Add(teamPlayer);
                    this.context.SaveChanges();
                }

            }
            catch (Exception)
            {

                throw;
            }
        }

        public List<Team> GetAllTeams()
        {
            try
            {
                var teams = this.context.Teams.Include(x => x.Players).ToList();
                return teams;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
