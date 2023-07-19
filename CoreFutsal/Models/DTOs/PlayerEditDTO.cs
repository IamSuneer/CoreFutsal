using System.ComponentModel.DataAnnotations;

namespace CoreFutsal.Models.DTOs
{
    public class PlayerEditDTO
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime DOB { get; set; }
        public string MobileNumber { get; set; }
        public string PermanentAddress { get; set; }
        public string? TemporaryAddress { get; set; }
        public string Nationality { get; set; }
        [Range(1,99)]
        public int? JerseyNumber { get; set; }
        public bool IsCaptain { get; set; } = false;

    }
}
