using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace BookMart.Areas.Identity.Data
{
    public class ApplicationUser : IdentityUser
    {

        [PersonalData]
        [Column(TypeName = "varchar(100)")]

        public string FirstName { get; set; }   

        [PersonalData]
        [Column(TypeName = "varchar(100)")]

        public string LastName { get; set; }

        [NotMapped]
        public virtual IEnumerable<string> Roles { get; set; }  
    }
}
