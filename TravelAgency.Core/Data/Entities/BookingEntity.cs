using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelAgency.Core.Data.Entities
{
    public class BookingEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public DateTime BookingDate { get; set; }

        public int TripPackageId { get; set; }
        public string TripPackageName { get; set; } = "";

        public string ClientUsername { get; set; } = "";

        public string StatusName { get; set; } = "";

        public string SelectedExtras { get; set; } = "";

        public double BasePrice { get; set; }
        public double TotalPrice { get; set; }
    }
}