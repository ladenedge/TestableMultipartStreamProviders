
using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
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
    public class TestableMultipartFormDataStreamProvider : TestableMultipartFileStreamProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestableMultipartFormDataStreamProvider"/> class.
        /// </summary>
        /// <param name="rootPath">The root path to which subparts with filename parameters are written.</param>
        public TestableMultipartFormDataStreamProvider(string rootPath)
            : base(rootPath, DefaultBufferSize) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestableMultipartFormDataStreamProvider"/> class.
        /// </summary>
        /// <param name="rootPath">The root path to which subparts with filename parameters are written.</param>
        /// <param name="bufferSize">The size of the buffer to be used in with file streams.</param>
        public TestableMultipartFormDataStreamProvider(string rootPath, int bufferSize)
            : base(rootPath, bufferSize, new FileWrap()) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestableMultipartFormDataStreamProvider"/> class.
        /// </summary>
        /// <param name="rootPath">The root path to which subparts with filename parameters are written.</param>
        /// <param name="bufferSize">The size of the buffer to be used in with file streams.</param>
        /// <param name="fileWrapper">A wrapper around <see cref="T:System.IO.File"/> allowing this class to be tested without accessing a filesystem.</param>
        public TestableMultipartFormDataStreamProvider(string rootPath, int bufferSize, IFile fileWrapper)
            : base(rootPath, bufferSize, fileWrapper) { }

        /// <summary>
        /// Gets a <see cref="NameValueCollection"/> of form data passed as part of the multipart form data.
        /// </summary>
        public NameValueCollection FormData { get { return _form_data; } }
        private NameValueCollection _form_data = new NameValueCollection();

        /// <summary>
        /// Reads the non-file contents as form data.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public override Task ExecutePostProcessingAsync()
        {
            var tasks = Contents.Where(c => c.Headers.ContentDisposition != null)
                                .Where(c => String.IsNullOrEmpty(c.Headers.ContentDisposition.FileName))
                                .Where(c => !String.IsNullOrEmpty(c.Headers.ContentDisposition.Name))
                                .Select(c => 
            {
                var name = Unquote(c.Headers.ContentDisposition.Name);
                return c.ReadAsStringAsync().ContinueWith(t => FormData.Add(name, t.Result));
            });

            return Task.WhenAll(tasks);
        }

        string Unquote(string s)
        {
            if (!s.StartsWith("\"") || !s.EndsWith("\""))
                return s;
            if (s.Length < 2)
                return s;

            return s.Substring(1, s.Length - 2);
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
            if (headers.ContentDisposition == null)
                throw new InvalidOperationException("Subpart does not contain a Content-Disposition header.");

            if (!String.IsNullOrEmpty(headers.ContentDisposition.FileName))
                return base.GetStream(parent, headers);

            return new MemoryStream();
        }
    }
}
