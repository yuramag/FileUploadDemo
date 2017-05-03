using System;
using System.Data.Entity;

namespace FileUploadDemoServer.DataAccess
{
    public class FileUploadDemoContext : DbContext
    {
        public FileUploadDemoContext()
        {
            Database.CommandTimeout = (int)TimeSpan.FromMinutes(5).TotalSeconds;
        }

        public FileUploadDemoContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
            Database.CommandTimeout = (int)TimeSpan.FromMinutes(5).TotalSeconds;
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Configurations.Add(new BlobFileConfiguration());
            modelBuilder.Configurations.Add(new BlobFileChunksConfiguration());
        }

        public new IDbSet<TEntity> Set<TEntity>() where TEntity : class
        {
            return base.Set<TEntity>();
        }
    }
}
