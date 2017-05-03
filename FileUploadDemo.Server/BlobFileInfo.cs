using System;
using System.Runtime.Serialization;

namespace FileUploadDemoServer
{
    public sealed class BlobFileInfo
    {
        [DataMember]
        public Guid BlobFileId { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public long Size { get; set; }

        [DataMember]
        public string CreatedBy { get; set; }

        [DataMember]
        public DateTime? CreatedOn { get; set; }
    }
}