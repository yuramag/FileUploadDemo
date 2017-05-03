using System.Data.Entity.ModelConfiguration;

namespace FileUploadDemoServer.DataAccess
{
    public class BlobFileChunksConfiguration : EntityTypeConfiguration<BlobFileChunks>
    {
        public BlobFileChunksConfiguration()
        {
            HasKey(x => new {x.BlobFileId, x.ChunkId});

            Property(x => x.Length).IsRequired();
            Property(x => x.Data).IsMaxLength();

            HasRequired(x => x.BlobFile).WithMany(x => x.Chunks).WillCascadeOnDelete(true);
        }
    }
}