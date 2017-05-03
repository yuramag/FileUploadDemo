using System.Data.Entity.ModelConfiguration;

namespace FileUploadDemoServer.DataAccess
{
    public class BlobFileConfiguration : EntityTypeConfiguration<BlobFile>
    {
        public BlobFileConfiguration()
        {
            HasKey(x => x.BlobFileId);

            Property(x => x.Name).HasMaxLength(255);
            Property(x => x.Description).HasMaxLength(255);
            Property(x => x.Size).IsRequired();
            Property(x => x.CreatedBy).HasMaxLength(255);
            Property(x => x.CreatedOn).IsOptional();

            HasMany(x => x.Chunks).WithRequired(x => x.BlobFile).WillCascadeOnDelete(true);
        }
    }
}