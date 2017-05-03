using System;
using System.Collections.Generic;
using System.IO;

namespace FileUploadDemoServer
{
    public sealed class MultiStream : Stream
    {
        private Stream m_stream;
        private readonly IEnumerator<Stream> m_streamEnum;
        private long m_length;
        private long m_position;
        private long m_minPosition;

        public MultiStream(IEnumerable<Stream> streams)
            : this(streams, long.MaxValue)
        {
        }

        public MultiStream(IEnumerable<Stream> streams, long length)
        {
            if (streams == null)
                throw new ArgumentNullException("streams");
            m_stream = null;
            m_length = length;
            m_position = 0;
            m_minPosition = 0;
            m_streamEnum = streams.GetEnumerator();
        }

        public override void Close()
        {
            if (m_stream != null)
            {
                m_stream.Dispose();
                m_stream = null;
            }
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    m_position = offset;
                    break;
                case SeekOrigin.Current:
                    m_position += offset;
                    break;
                case SeekOrigin.End:
                    m_position = m_length - offset;
                    break;
            }

            if (m_position > m_length)
                m_position = m_length;

            if (m_position < m_minPosition)
            {
                m_position = m_minPosition;
                throw new NotSupportedException("Cannot seek backwards");
            }

            return m_position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var result = 0;

            while (true)
            {
                if (m_stream == null)
                {
                    if (!m_streamEnum.MoveNext())
                    {
                        m_length = m_position;
                        break;
                    }
                    m_stream = m_streamEnum.Current;
                }

                if (m_position >= m_minPosition + m_stream.Length)
                {
                    m_minPosition += m_stream.Length;
                    m_stream.Dispose();
                    m_stream = null;
                }
                else
                {
                    m_stream.Position = m_position - m_minPosition;
                    var bytesRead = m_stream.Read(buffer, offset, count);
                    result += bytesRead;
                    offset += bytesRead;
                    m_position += bytesRead;
                    if (bytesRead < count)
                    {
                        count -= bytesRead;
                        m_minPosition += m_stream.Length;
                        m_stream.Dispose();
                        m_stream = null;
                    }
                    else
                        break;
                }
            }

            return result;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return m_length; }
        }

        public override long Position
        {
            get { return m_position; }
            set { Seek(value, SeekOrigin.Begin); }
        }
    }
}