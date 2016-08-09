using System.ComponentModel.DataAnnotations;

namespace AssetManagement.Common
{
    public enum Status
    {
        Ok,

        [Display(Name = "Needs Attention!")]
        NeedsAttention,

        Broken
    }
}