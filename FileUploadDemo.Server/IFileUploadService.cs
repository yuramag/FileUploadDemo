using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;

namespace FileUploadDemoServer
{
    [ServiceContract]
    public interface IFileUploadService
    {
        [OperationContract]
        Task<Guid> CreateBlobFileAsync(string name, string description, long size, string createdBy);

        [OperationContract]
        Task AddBlobFileChunkAsync(Guid blobFileId, int chunkId, byte[] data);

        [OperationContract]
        Task DeleteBlobFileAsync(Guid blobFileId);

        [OperationContract]
        Task<IEnumerable<BlobFileInfo>> GetBlobFilesAsync();

        [OperationContract]
        Task<string> ProcessFileAsync(Guid blobFileId);

        [OperationContract]
        Task SaveFileAsAsync(Guid blobFileId, string fileName);
    }
}
