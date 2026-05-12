using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelAgency.Core.Data.Entities
{
    /// <summary>Favorite pachet salvat de un client (identificat prin username, ca la rezervări).</summary>
    public class ClientPackageFavoriteEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(128)]
        public string ClientUsername { get; set; } = "";

        public int TripPackageId { get; set; }

        public DateTime SavedAtUtc { get; set; }
    }
}
