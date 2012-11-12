
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Net.Http.Headers;
using SystemInterface.IO;
using SystemWrapper.IO;

namespace System.Net.Http
{
    /// <summary>
    /// A stream provider suited for use with multipart/form-data messages.
    /// The stream provider looks at the <b>Content-Disposition</b> header
    /// field and creates an output stream based on the presence of a
    /// <b>filename</b> parameter.
    /// </summary>
    public class TestableMultipartFileStreamProvider : MultipartStreamProvider
    {
        /// <summary>
        /// The default buffer size for file writes.
        /// </summary>
        public const int DefaultBufferSize = 4096;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestableMultipartFileStreamProvider"/> class.
        /// </summary>
        /// <param name="rootPath">The root path to which body parts with filename parameters are written.</param>
        public TestableMultipartFileStreamProvider(string rootPath)
            : this(rootPath, DefaultBufferSize) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestableMultipartFileStreamProvider"/> class.
        /// </summary>
        /// <param name="rootPath">The root path to which body parts with filename parameters are written.</param>
        /// <param name="bufferSize">The size of the buffer to be used in with file streams.</param>
        public TestableMultipartFileStreamProvider(string rootPath, int bufferSize)
            : this(rootPath, bufferSize, new FileWrap()) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestableMultipartFileStreamProvider"/> class.
        /// </summary>
        /// <param name="rootPath">The root path to which body parts with filename parameters are written.</param>
        /// <param name="bufferSize">The size of the buffer to be used in with file streams.</param>
        /// <param name="fileWrapper">A wrapper around <see cref="T:System.IO.File"/> allowing this class to be tested without accessing a filesystem.</param>
        public TestableMultipartFileStreamProvider(string rootPath, int bufferSize, IFile fileWrapper)
        {
            RootPath = rootPath;
            BufferSize = bufferSize;
            FileWrapper = fileWrapper;
        }

        /// <summary>
        /// Gets the root path to which body parts with filename parameters are written.
        /// </summary>
        public string RootPath { get; private set; }

        /// <summary>
        /// Gets the number of bytes buffered for writes to the file.
        /// </summary>
        public int BufferSize { get; private set; }

        /// <summary>
        /// Gets the multipart file data.
        /// </summary>
        public Collection<MultipartFileData> FileData { get { return _file_data; } }
        private Collection<MultipartFileData> _file_data = new Collection<MultipartFileData>();

        private IFile FileWrapper { get; set; }

        /// <summary>
        /// Gets the name of the local file which will be combined with the root path
        /// to create an absolute file name where the contents of the current MIME
        /// body part will be stored.
        /// </summary>
        /// <param name="headers">The headers for the current MIME body part.</param>
        /// <returns>A relative filename with no path component.</returns>
        public virtual string GetLocalFileName(HttpContentHeaders headers)
        {
            if (headers == null)
                throw new ArgumentNullException("headers");

            return String.Format(CultureInfo.InvariantCulture, "BodyPart_{0}", Guid.NewGuid());
        }

        /// <summary>
        /// Gets the stream instance to which the message body part is written.
        /// </summary>
        /// <param name="parent">The HTTP content that contains this body part.</param>
        /// <param name="headers">Header fields describing the body part.</param>
        /// <returns>The <see cref="Stream"/> instance where the message body part is written.</returns>
        public override Stream GetStream(HttpContent parent, HttpContentHeaders headers)
        {
            if (headers == null)
                throw new ArgumentNullException("headers");

            var filename = GetLocalFileName(headers);
            var fullpath = Path.Combine(RootPath, filename);

            var data = new MultipartFileData(headers, fullpath);
	        FileData.Add(data);
	        
            var stream_wrapper = FileWrapper.Create(fullpath, BufferSize, FileOptions.Asynchronous);
            return stream_wrapper.StreamInstance;
        }
    }
}
