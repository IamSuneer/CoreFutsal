using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Xml.Linq;
using System.Security;

namespace CoreFutsal.Models.ViewModels
{
    public class UserRegisterViewModel 
    {
        [Display(Name = "First Name")]
        [StringLength(50, MinimumLength = 3)]
        [Required]
        public string FirstName { get; set; }

        [StringLength(50, MinimumLength = 3)]
        [Display(Name = "Last Name")]
        [Required]
        public string LastName { get; set; }

        [Display(Name = "Date of Birth")]
        [Required]
        public DateTime DOB { get; set; }

        [Display(Name = "Mobile Number")]
        [StringLength(10, MinimumLength = 10)]
        [Required]
        public string MobileNumber { get; set; }

        [Display(Name = "Permanent Address")]
        [Required]
        public string PermanentAddress { get; set; }

        [Display(Name = "Temporary Address")]
        public string? TemporaryAddress { get; set; }

        public string Nationality { get; set; }

        [Required]
        [Display(Name = "User Name")]
        public string UserName { get; set; }

        [Display(Name = "Email Address")]
        [RegularExpression(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){3})+)$", ErrorMessage = "Please enter a valid email address")]
        [Required]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string? ConfirmPassword { get; set; }
    }
}
