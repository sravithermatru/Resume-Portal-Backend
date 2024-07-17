using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Resume_Selector_Page.Models
{
    public class Recruiter : IdentityUser

    {
        
        public string Name { get; set; }
        public string Company { get; set; }
        [Required]
        public string PreferencesTechnology { get; set; }
        
        


    }
}
