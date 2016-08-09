using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AssetManagement.Common
{
    public class Asset
    {
        public int Id { get; set; }

        public int AssetTypeId { get; set; }

        [DisplayName("Asset Name")]
        [StringLength(100)]
        public string Name { get; set; }

        public virtual AssetType AssetType { get; set; }
        public ICollection<Log> Logs { get; set; }
    }
}