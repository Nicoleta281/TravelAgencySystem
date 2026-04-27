using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelAgency.Core.Data.Entities
{
    public class PasswordResetLinkTokenEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int UserId { get; set; }

        /// <summary>Base64(SHA256(token)); never store plaintext token.</summary>
        public string TokenHash { get; set; } = "";

        public DateTime CreatedAtUtc { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime? ConsumedAtUtc { get; set; }
    }
}

