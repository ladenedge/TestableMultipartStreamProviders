
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Ploeh.AutoFixture;
using SystemInterface.IO;

namespace System.Net.Http.Tests
{
    public class TestableMultipartFormDataStreamProvider_Tests
    {
        Fixture Fixtures = new Fixture();

        [Test]
        public void Constructor1_SetsRootPath()
        {
            var path = Fixtures.CreateAnonymous<string>();
            var provider = new TestableMultipartFormDataStreamProvider(path);

            Assert.That(provider.RootPath, Is.EqualTo(path));
        }

        [Test]
        public void Constructor2_SetsRootPath()
        {
            var path = Fixtures.CreateAnonymous<string>();
            var size = Fixtures.CreateAnonymous<int>();
            var provider = new TestableMultipartFormDataStreamProvider(path, size);

            Assert.That(provider.RootPath, Is.EqualTo(path));
        }

        [Test]
        public void Constructor2_SetsBufferSize()
        {
            var path = Fixtures.CreateAnonymous<string>();
            var size = Fixtures.CreateAnonymous<int>();
            var provider = new TestableMultipartFormDataStreamProvider(path, size);

            Assert.That(provider.BufferSize, Is.EqualTo(size));
        }

        [Test]
        public void Constructor3_SetsRootPath()
        {
            var path = Fixtures.CreateAnonymous<string>();
            var size = Fixtures.CreateAnonymous<int>();
            var provider = new TestableMultipartFormDataStreamProvider(path, size, null);

            Assert.That(provider.RootPath, Is.EqualTo(path));
        }

        [Test]
        public void Constructor3_SetsBufferSize()
        {
            var path = Fixtures.CreateAnonymous<string>();
            var size = Fixtures.CreateAnonymous<int>();
            var provider = new TestableMultipartFormDataStreamProvider(path, size, null);

            Assert.That(provider.BufferSize, Is.EqualTo(size));
        }

        [Test]
        public void ExecutePostProcessingAsync_SavesFormSubpart()
        {
            var provider_factory = Fixtures.CreateAnonymous<FormDataStreamProviderFactory>();
            var content_factory = Fixtures.CreateAnonymous<MultipartFormDataContentFactory>();
            var provider = provider_factory.NewProvider();

            var task = content_factory.NewContent().ReadAsMultipartAsync(provider).ContinueWith<HttpResponseMessage>(t =>
            {
                Assume.That(t.IsFaulted, Is.False);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
            task.Wait();

            Assert.That(provider.FormData.Get(content_factory.SubPart1Name), Is.EqualTo(content_factory.SubPart1Value));
        }

        Tuple<string, string>[] QuotedStrings = new[] {
            Tuple.Create("unquoted", "unquoted"),
            Tuple.Create("\"quoted\"", "quoted"),
        };

        [Test]
        public void ExecutePostProcessingAsync_HandlesQuotes([ValueSource("QuotedStrings")] Tuple<string, string> name)
        {
            var provider_factory = Fixtures.CreateAnonymous<FormDataStreamProviderFactory>();
            var provider = provider_factory.NewProvider();

            var content = new MultipartFormDataContent();
            content.Headers.Add("Content-Disposition", "form-data");
            content.Headers.ContentDisposition.Name = name.Item1;

            provider.Contents.Add(content);
            provider.ExecutePostProcessingAsync().Wait();

            Assert.That(provider.FormData.AllKeys.First(), Is.EqualTo(name.Item2));
        }

        [Test]
        public void ExecutePostProcessingAsync_SkipsEmptyNames([Values(null, "")] string name)
        {
            var provider_factory = Fixtures.CreateAnonymous<FormDataStreamProviderFactory>();
            var provider = provider_factory.NewProvider();

            var content = new MultipartFormDataContent();
            content.Headers.Add("Content-Disposition", "form-data");
            content.Headers.ContentDisposition.Name = name;

            provider.Contents.Add(content);
            provider.ExecutePostProcessingAsync().Wait();

            Assert.That(provider.FormData.AllKeys, Is.Empty);
        }

        [Test]
        public void ExecutePostProcessingAsync_DoesNotSaveFileSubpart()
        {
            var provider_factory = Fixtures.CreateAnonymous<FormDataStreamProviderFactory>();
            var content_factory = Fixtures.CreateAnonymous<MultipartFormDataContentFactory>();
            var provider = provider_factory.NewProvider();

            var task = content_factory.NewContent().ReadAsMultipartAsync(provider).ContinueWith<HttpResponseMessage>(t =>
            {
                Assume.That(t.IsFaulted, Is.False);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
            task.Wait();

            Assert.That(provider.FormData.AllKeys, Has.None.EqualTo(content_factory.SubPart2Name));
        }

        [Test]
        public void GetStream_SupportsFileParts()
        {
            var provider_factory = Fixtures.CreateAnonymous<FormDataStreamProviderFactory>();
            var content_factory = Fixtures.CreateAnonymous<MultipartFormDataContentFactory>();
            var provider = provider_factory.NewProvider();

            var task = content_factory.NewContent().ReadAsMultipartAsync(provider).ContinueWith<HttpResponseMessage>(t =>
            {
                Assume.That(t.IsFaulted, Is.False);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
            task.Wait();

            var file = provider.FileData.First();
            Assert.That(file.LocalFileName, Is.StringContaining(provider_factory.Path));
        }

        class FormDataStreamProviderFactory
        {
            Fixture Fixtures = new Fixture();

            public string Path { get; set; }
            public int BufferSize { get; set; }
            public Mock<IFile> FileMock { get; set; }
            public Mock<IFileStream> StreamMock { get; set; }

            public TestableMultipartFormDataStreamProvider NewProvider()
            {
                FileMock.Setup(f => f.Create(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<FileOptions>())).Returns(StreamMock.Object);
                StreamMock.SetupGet(fs => fs.StreamInstance).Returns(new MemoryStream());
                return new TestableMultipartFormDataStreamProvider(Path, BufferSize, FileMock.Object);
            }
        }

        class MultipartFormDataContentFactory
        {
            Fixture Fixtures = new Fixture();

            public string SubPart1Name { get; set; }
            public string SubPart1Value { get; set; }
            public string SubPart2Name { get; set; }
            public string SubPart2Value { get; set; }
            public string SubPart2Filename { get; set; }

            public MultipartFormDataContent NewContent()
            {
                var content = new MultipartFormDataContent(Fixtures.CreateAnonymous<string>());
                var subpart1 = new ByteArrayContent(Encoding.UTF8.GetBytes(SubPart1Value));
                var subpart2 = new ByteArrayContent(Encoding.UTF8.GetBytes(SubPart2Value));
                content.Add(subpart1, SubPart1Name);
                content.Add(subpart2, SubPart2Name, SubPart2Filename);
                return content;
            }
        }
    }
}
