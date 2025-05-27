using System.Collections.Generic;

namespace AspNetCoreMvcTemplate.ViewModels
{
    public class UserViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public List<string> AvailableRoles { get; set; } = new List<string> { "Admin", "Accountant", "Auditor", "Manager" };
        public List<string> SelectedRoles { get; set; } = new List<string>();
    }
}
