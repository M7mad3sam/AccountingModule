using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;

namespace AspNetCoreMvcTemplate.Models
{
    public class ApplicationUser : IdentityUser
    {
    [Required]
    [PersonalData]
    public string FullName { get; set; } = string.Empty;

    [PersonalData]
    [DataType(DataType.Date)]
    public DateTime DateOfBirth { get; set; }
    }
}
