using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FileUploadDemoServer.DataAccess;

namespace FileUploadDemoServer
{
    public class FileUploadService : IFileUploadService
    {
        static FileUploadService()
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<FileUploadDemoContext, Migrations.Configuration>());
        }

        public async Task<Guid> CreateBlobFileAsync(string name, string description, long size, string createdBy)
        {
            var blobFile = new BlobFile
            {
                BlobFileId = Guid.NewGuid(),
                Name = name,
                Description = description,
                Size = size,
                CreatedBy = createdBy,
                CreatedOn = DateTime.UtcNow
            };

            using (var context = new FileUploadDemoContext())
            {
                context.Set<BlobFile>().Add(blobFile);
                await context.SaveChangesAsync();
            }

            return blobFile.BlobFileId;
        }

        public async Task AddBlobFileChunkAsync(Guid blobFileId, int chunkId, byte[] data)
        {
            var chunk = new BlobFileChunks
            {
                BlobFileId = blobFileId,
                ChunkId = chunkId,
                Length = data.Length,
                Data = data
            };

            using (var context = new FileUploadDemoContext())
            {
                context.Set<BlobFileChunks>().Add(chunk);
                await context.SaveChangesAsync();
            }
        }

        public async Task DeleteBlobFileAsync(Guid blobFileId)
        {
            using (var context = new FileUploadDemoContext())
            {
                var blobFile = await context.Set<BlobFile>().SingleAsync(x => x.BlobFileId == blobFileId);
                context.Set<BlobFile>().Remove(blobFile);
                await context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<BlobFileInfo>> GetBlobFilesAsync()
        {
            using (var context = new FileUploadDemoContext())
            {
                var result = await context.Set<BlobFile>().OrderBy(x => x.CreatedOn).ToListAsync();
                return result.Select(x => new BlobFileInfo
                {
                    BlobFileId = x.BlobFileId,
                    Name = x.Name,
                    Description = x.Description,
                    Size = x.Size,
                    CreatedBy = x.CreatedBy,
                    CreatedOn = x.CreatedOn
                });
            }
        }

        public async Task<string> ProcessFileAsync(Guid blobFileId)
        {
            List<int> chunks;
            using (var context = new FileUploadDemoContext())
                chunks = await context.Set<BlobFileChunks>().Where(x => x.BlobFileId == blobFileId).OrderBy(x => x.ChunkId).Select(x => x.ChunkId).ToListAsync();

            var result = 0;

            using (var stream = new MultiStream(GetBlobStreams(blobFileId, chunks)))
            {
                foreach (var chunk in stream.GetByteChunks(1024))
                    result = (result*31) ^ ComputeHash(chunk);
            }
            
            return string.Format("File Hash Code is: {0}", result);
        }


        public async Task SaveFileAsAsync(Guid blobFileId, string fileName)
        {
            List<int> chunks;
            using (var context = new FileUploadDemoContext())
                chunks = await context.Set<BlobFileChunks>().Where(x => x.BlobFileId == blobFileId).OrderBy(x => x.ChunkId).Select(x => x.ChunkId).ToListAsync();

            if (File.Exists(fileName))
                File.Delete(fileName);

            using (var file = File.OpenWrite(fileName))
            using (var stream = new MultiStream(GetBlobStreams(blobFileId, chunks)))
                stream.CopyTo(file);
        }

        private static IEnumerable<Stream> GetBlobStreams(Guid blobFileId, IEnumerable<int> chunks)
        {
            foreach (var chunkId in chunks)
            {
                using (var context = new FileUploadDemoContext())
                {
                    var chunk = context.Set<BlobFileChunks>().Single(x => x.BlobFileId == blobFileId && x.ChunkId == chunkId);
                    yield return new MemoryStream(chunk.Data);
                }
            }
        }

        private static int ComputeHash(params byte[] data)
        {
            unchecked
            {
                var result = 0;
                foreach (var b in data)
                    result = (result * 31) ^ b;
                return result;
            }
        }
    }
}