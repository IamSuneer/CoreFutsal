using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CoreFutsal.Models
{
    public class TeamPlayer
    {
        [Key]
        [ForeignKey("TeamId")]
        public Guid TeamId { get; set; }

        [Key]
        [ForeignKey("PlayerId")]
        public Guid PlayerId { get; set; }
    }
}
