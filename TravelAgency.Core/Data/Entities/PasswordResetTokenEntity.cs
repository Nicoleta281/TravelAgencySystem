using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelAgency.Core.Data.Entities
{
    public class PasswordResetTokenEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int UserId { get; set; }

        /// <summary>Base64(SHA256(code + salt)) or similar; never store plaintext code.</summary>
        public string CodeHash { get; set; } = "";

        public DateTime CreatedAtUtc { get; set; }
        public DateTime ExpiresAtUtc { get; set; }

        public int Attempts { get; set; }
        public DateTime? ConsumedAtUtc { get; set; }
    }
}

