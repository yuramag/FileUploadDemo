using System;

namespace FileUploadDemoServer.DataAccess
{
    public class BlobFileChunks
    {
        public Guid BlobFileId { get; set; }
        public int ChunkId { get; set; }
        public int Length { get; set; }
        public byte[] Data { get; set; }

        public virtual BlobFile BlobFile { get; set; }
    }
}