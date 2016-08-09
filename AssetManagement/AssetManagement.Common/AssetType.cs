using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AssetManagement.Common
{
    public class AssetType
    {
        public int Id { get; set; }

        [DisplayName("Asset Type")]
        [StringLength(100)]
        public string Name { get; set; }


        public virtual ICollection<Asset> Assets { get; set; }
    }
}
