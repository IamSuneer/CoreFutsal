using System.ComponentModel.DataAnnotations;

namespace CoreFutsal.Models
{
    public class Team
    {
        public Guid TeamId { get; set; }

        [Required]
        [Display(Name = "Team Name")]
        [StringLength(30, MinimumLength = 4)]
        public string TeamName { get; set; }

        [Required]
        [StringLength(3, MinimumLength =2)]
        public string Abbreviations{ get; set; }

        public string? Image { get; set; }

        [StringLength(50, MinimumLength = 5)]
        public string? TeamDescription { get; set; }

        [Required]
        public string TeamAddress { get; set; }

        public bool Status { get; set; } = true;

        [Required]
        [Range(1, 9)]
        public ICollection<Player>? Players { get; set; }

        public int PlayerNumbers
        {
            get
            {
                return Players.Count();
            }
        }
    }
}
