using System;
using System.Collections.Generic;

namespace FileUploadDemoServer.DataAccess
{
    public class BlobFile
    {
        public Guid BlobFileId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public long Size { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }

        public virtual ICollection<BlobFileChunks> Chunks { get; set; }
    }
}