using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelAgency.Core.Data.Entities
{
    public class TripPackageEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Name { get; set; } = "";
        public double Price { get; set; }

        // Season
        public string SeasonName { get; set; } = "";
        public DateTime SeasonStartDate { get; set; }
        public DateTime SeasonEndDate { get; set; }

        // Store types as string (simple + robust)
        public string TransportType { get; set; } = "";
        public string StayType { get; set; } = "";

        // Extras as CSV (ex: "Breakfast,Guide")
        public string ExtraServices { get; set; } = "";
    }
}