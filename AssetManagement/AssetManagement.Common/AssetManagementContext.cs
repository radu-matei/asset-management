using System.Data.Entity;

namespace AssetManagement.Common
{
    public class AssetManagementContext : DbContext
    {
        public AssetManagementContext() : base("name=AssetManagementContext")
        {
        }

        public DbSet<AssetType> AssetTypes { get; set; }
        public DbSet<Asset> Assets { get; set; }
        public DbSet<Log> Logs { get; set; }
    }
}
