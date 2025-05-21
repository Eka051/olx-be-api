using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace olx_be_api.Models
{
    public class Location
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public int? ProvinceId { get; set; }
        public int? CityId { get; set; }
        public int? DistrictId { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public Province? Province { get; set; }
        public City? City { get; set; }
        public District? District { get; set; }

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
