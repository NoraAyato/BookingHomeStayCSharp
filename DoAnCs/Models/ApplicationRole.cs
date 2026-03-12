using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace DoAnCs.Models
{
    public class ApplicationRole : IdentityRole
    {
        [MaxLength(256)]
        public string? Description { get; set; }
    }
}
