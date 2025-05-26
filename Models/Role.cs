using System.ComponentModel.DataAnnotations;

namespace olx_be_api.Models
{
    public class Role
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
