using CoreFutsal.DAL;
using CoreFutsal.Models;
using CoreFutsal.Models.DTOs;
using CoreFutsal.Models.ViewModels;

namespace CoreFutsal.Service
{
    public class PlayerService : IPlayerService
    {
        private readonly FutsalContext context;

        public PlayerService(FutsalContext context)
        {
            this.context = context;
        }

        public List<Player> GetAllPlayers()
        {
            return this.context.Players.ToList();
        }

        public void AddPlayer(UserRegisterViewModel model, Guid id)
        {
            try
            {
                var player = new Player()
                {
                    PlayerId = id,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PermanentAddress = model.PermanentAddress,
                    TemporaryAddress = model.TemporaryAddress,
                    MobileNumber = model.MobileNumber,
                    DOB = model.DOB,
                    Nationality = model.Nationality
                };
                this.context.Players.Add(player);
                this.context.SaveChanges();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void UpdatePlayer(PlayerEditDTO model, Guid id)
        {
            try
            {
                var player = new Player()
                {
                    PlayerId = id,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PermanentAddress = model.PermanentAddress,
                    TemporaryAddress = model.TemporaryAddress,
                    MobileNumber = model.MobileNumber,
                    DOB = model.DOB,
                    Nationality = model.Nationality,
                    JerseyNumber = model.JerseyNumber,
                    IsCaptain = model.IsCaptain
                };
                this.context.Players.Update(player);
                this.context.SaveChanges();
            }
            catch (Exception)
            {

                throw;
            }
        }

        public bool CheckPlayer(Guid id)
        {
            return this.context.Players.Any(e => e.PlayerId == id);
        }
    }
}
