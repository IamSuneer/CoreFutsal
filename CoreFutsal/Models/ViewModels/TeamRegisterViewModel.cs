using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace CoreFutsal.Models.ViewModels
{
    public class TeamRegisterViewModel
    {
        [Required]
        [Display(Name = "Team Name")]
        [StringLength(30, MinimumLength = 4)]
        public string TeamName { get; set; }

        [StringLength(50, MinimumLength = 5)]
        public string? TeamDescription { get; set; }

        [Required]
        public string TeamAddress { get; set; }
    }
}
