using System;
using System.Collections.Generic;
using System.IO;

namespace FileUploadDemoClient
{
    public static class StreamExtensions
    {
        public static IEnumerable<byte[]> GetByteChunks(this Stream stream, int length)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            var buffer = new byte[length];
            int count;
            while ((count = stream.Read(buffer, 0, buffer.Length)) != 0)
            {
                var result = new byte[count];
                Array.Copy(buffer, 0, result, 0, count);
                yield return result;
            }
        }
    }
}