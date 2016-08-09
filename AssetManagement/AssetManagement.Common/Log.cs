using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AssetManagement.Common
{
    public class Log
    {
        public int Id { get; set; }

        public int AssetId { get; set; }

        [DisplayName("Temperature in C")]
        public double Temperature { get; set; }

        [DisplayName("Pressure in Pa")]
        public double Pressure { get; set; }

        [StringLength(2083)]
        [DisplayName("Full-size Image")]
        public string ImageUrl { get; set; }


        [StringLength(2083)]
        [DisplayName("Thumbnail")]
        public string ThumbnailUrl { get; set; }

        public Status Status { get; set; }

        public virtual Asset Asset { get; set; }
    }
}