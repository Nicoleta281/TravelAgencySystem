using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelAgency.Core.Data.Entities
{
    public class UserMessageEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(128)]
        public string FromUsername { get; set; } = "";

        [Required]
        [MaxLength(128)]
        public string ToUsername { get; set; } = "";

        [Required]
        [MaxLength(2000)]
        public string Body { get; set; } = "";

        public DateTime SentAtUtc { get; set; }

        public bool IsRead { get; set; }
    }
}
