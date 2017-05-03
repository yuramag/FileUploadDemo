<h1>Large Data Upload via WCF</h1>

<h2>Background</h2>

<p>Transferring large volumes of data over the wire has always been a challenge. There are several approaches exist that try solving this problem from different angles. Each of those solutions suggests different usage scenarios and has pros and cons. Here are 3 major options:</p>

<ul>
	<li><strong>FTP</strong>. Although FTP is fast and efficient, it is not easy to use. It does not have well designed API or object model around it, it has issues with firewall, and requires long sessions to be opened during data transfer, which are not supposed to be interrupted. In addition, FTP infrastructure requires configuration and maintenance efforts.</li>
	<li><strong>Streaming</strong>. Streaming is efficient solution for transferring large amounts of data over HTTP or TCP. However, it does not support reliable messaging and not very well scalable for high traffic scenarios.</li>
	<li><strong>Chunking</strong>. This solution, among all others, has some overhead associated with chunking data and reconstituting it back into single stream. However, most parts of the overhead may be eliminated by having efficient architectural design. Chunking solution is ideal for large data transfer in high traffic scenarios, can support reliable messaging, security on both message and transport levels, offers efficient memory utilization, and suggests a straightforward way of implementing partial uploads in case of connection failures.</li>
</ul>

<p>In this article, we will walk through one of the possible implementations of Chunking solution.</p>

<h2>Introduction</h2>

<p>Current solution consists of Client (WPF UI application) and Server (WCF Console application). On the server side, we have WCF Service exposing several methods to manipulate data files. It is using Entity Framework Code First approach to talk to SQL Server to persist data.</p>

<p>On the database side, we got two tables - <code>BlobFiles</code> and <code>BlobFileChunks</code>. Every record in <code>BlobFiles</code> table corresponds to the description of uploaded file and includes its Id (Guid), Name, Description, Size, User Id created, and creation timestamp. <code>BlobFileChunks</code>, on the other hand, is a detail table referencing <code>BlobFiles</code> by foreign key. Every record in this table represents a chunk of data in binary format as well as its sequential number (position) in the original file stored in the <code>ChunkId</code> column.</p>

<p>On the client side, we have a grid displaying Data Files pulled from the database, and few buttons allowing to refresh data, upload a new file, remove existing file, as well as calculate file hash code and save file to disk. The latter two actions are purely demonstrational and serve the purpose of emulating actual consumption of file content on the server side.</p>

<h2>Using the Code</h2>

<p>Before using the code, make sure that connection settings configured correctly in server&#39;s <code>App.config</code> file. By default, it is using local instance of SQL Server with Integrated Security:</p>

<pre>
&lt;entityFramework&gt;
  &lt;defaultConnectionFactory type=&quot;System.Data.Entity.Infrastructure.SqlConnectionFactory, EntityFramework&quot;&gt;
    &lt;parameters&gt;
      &lt;parameter value=&quot;<strong>Data Source=.;Integrated Security=True;MultipleActiveResultSets=True;</strong>&quot; /&gt;
    &lt;/parameters&gt;
  &lt;/defaultConnectionFactory&gt;
  &lt;providers&gt;
    &lt;provider invariantName=&quot;System.Data.SqlClient&quot; 
              type=&quot;System.Data.Entity.SqlServer.SqlProviderServices, EntityFramework.SqlServer&quot; /&gt;
  &lt;/providers&gt;
&lt;/entityFramework&gt;</pre>

<p>Since project contains EF configured with automatic DB Migrations, the database is going to be created automatically, on the first run.</p>

<p>Without getting too deep into details, let&#39;s focus on major points of the solution - the mechanism of uploading chunks and later combining them together.</p>

<h2>Splitting into Chunks</h2>

<p>On the client side, the following code used to upload chunks to the server:</p>

<pre>
using (var service = new FileUploadServiceClient())
{
  var fileId = await service.CreateBlobFileAsync(Path.GetFileName(fileName),
    fileName, new FileInfo(fileName).Length, Environment.UserName);

  using (var stream = File.OpenRead(fileName))
  {
    var edb = new ExecutionDataflowBlockOptions {BoundedCapacity = 5, MaxDegreeOfParallelism = 5};

    var ab = new ActionBlock&lt;Tuple&lt;byte[], int&gt;&gt;(x =&gt; service.AddBlobFileChunkAsync(fileId, x.Item2, x.Item1), edb);

    foreach (var item in stream.GetByteChunks(chunkSize).Select((x, i) =&gt; Tuple.Create(x, i)))
      await ab.SendAsync(item);

    ab.Complete();

    await ab.Completion;
  }
}</pre>

<p>As you can see, <code>ActionBlock</code> class (part of great library <a href="http://msdn.microsoft.com/en-us/library/hh228603(v=vs.110).aspx">TPL Dataflow</a>) has been used in the code, which abstracts away asynchronous parallel processing, allowing to send maximum 5 chunks at a time (<code>MaxDegreeOfParallelism</code>) and maintaining a buffer of 5 chunks in the memory (<code>BoundedCapacity</code>).</p>

<p>By calling <code>CreateBlobFileAsync</code>, we create master record with file details and obtain <code>BlobFileId</code>. We then open a File Stream, call an extension method <code>GetByteChunks</code> that returns an <code>IEnumerable&lt;byte[]&gt;</code>. This enumerable is evaluated on the fly and every element contains a byte array of specified size (obviously, the last element may contain less data). Chunk size by default is set to 2MB and can be adjusted according to the usage scenario. Those chunks together with their sequential IDs are sent to the server in parallel via <code>AddBlobFileChunkAsync</code> method. Note that the chunks do not necessarily have to be sent one after another as long as their sequential IDs represent their actual positions in the file.</p>

<p>Implementation of <code>GetByteChunks</code> extension method is presented below:</p>

<pre>
 public static IEnumerable&lt;byte[]&gt; GetByteChunks(this Stream stream, int length)
{
  if (stream == null)
    throw new ArgumentNullException(&quot;stream&quot;);

  var buffer = new byte[length];
  int count;
  while ((count = stream.Read(buffer, 0, buffer.Length)) != 0)
  {
    var result = new byte[count];
    Array.Copy(buffer, 0, result, 0, count);
    yield return result;
  }
}</pre>

<h2>Combining the Chunks</h2>

<p>Let&#39;s explore <code>ProcessFileAsync</code> method on the server side:</p>

<pre>
 public async Task&lt;string&gt; ProcessFileAsync(Guid blobFileId)
{
  List&lt;int&gt; chunks;
  using (var context = new FileUploadDemoContext())
  {
    chunks = await context.Set&lt;BlobFileChunks&gt;()
      .Where(x =&gt; x.BlobFileId == blobFileId)
      .OrderBy(x =&gt; x.ChunkId)
      .Select(x =&gt; x.ChunkId)
      .ToListAsync();
  }

  var result = 0;

  using (var stream = new MultiStream(GetBlobStreams(blobFileId, chunks)))
  {
    foreach (var chunk in stream.GetByteChunks(1024))
      result = (result*31) ^ ComputeHash(chunk);
  }

  return string.Format(&quot;File Hash Code is: {0}&quot;, result);
}</pre>

<p>This method reads entire file content in a lazy manner and calculates its hash code. First, we retrieve all chunks associated with given <code>BlobFileId</code> sorted by <code>ChunkId</code> from database. Those chunk IDs then passed to <code>GetByteChunks</code> helper method, which retrieves actual binary content of those chunks from database on demand, one after another.</p>

<p>The key role in combining chunks together plays <code>MultiStream</code> class described below.</p>

<h2>MultiStream Class</h2>

<p><code>MultiStream</code> is inhereted from standard <code>Stream</code> class and represents a read-only stream with ability to seek forward. The <code>Seek</code> method ensures that position never gets backwards:</p>

<pre>
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

  if (m_position &gt; m_length)
    m_position = m_length;

  if (m_position &lt; m_minPosition)
  {
    m_position = m_minPosition;
    throw new NotSupportedException(&quot;Cannot seek backwards&quot;);
  }

  return m_position;
}</pre>

<p>In the <code>Read</code> method, we pull chunks on &quot;as needed&quot; basis until we reach the last one. This way only one chunk at a time is held in memory, which very efficient and scalable:</p>

<pre>
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

    if (m_position &gt;= m_minPosition + m_stream.Length)
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
      if (bytesRead &lt; count)
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
}</pre>

<h2>Conclusion</h2>

<ul>
	<li>Current approach requires both <code>Client</code> and <code>Server</code> to be aware of protocol</li>
	<li>Solution is well suited for intranet and internet scenarios</li>
	<li>DB persistence layer can be easily replaced by File System if needed</li>
	<li>Reliable messaging can be easily implemented on top of current architecture</li>
	<li>There is a lot of room for improvement, so please feel free to post your suggestions or concerns.</li>
</ul>
