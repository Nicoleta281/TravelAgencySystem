using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelAgency.Core.Data.Entities
{
    public class UserEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Username { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string RoleName { get; set; } = "";
        public bool IsActive { get; set; } = true;
    }
}