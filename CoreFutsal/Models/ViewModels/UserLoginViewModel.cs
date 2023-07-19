using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace CoreFutsal.Models.ViewModels
{
    public class UserLoginViewModel
    {
        [Required]
        [Display(Name = "User Name")]
        public string UserName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
