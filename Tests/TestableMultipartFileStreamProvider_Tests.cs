
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Ploeh.AutoFixture;
using SystemInterface.IO;

namespace System.Net.Http.Tests
{
    public class TestableMultipartFileStreamProvider_Tests
    {
        Fixture Fixtures = new Fixture();

        [Test]
        public void Constructor1_SetsRootPath()
        {
            var path = Fixtures.CreateAnonymous<string>();
            var provider = new TestableMultipartFileStreamProvider(path);

            Assert.That(provider.RootPath, Is.EqualTo(path));
        }

        [Test]
        public void Constructor2_SetsRootPath()
        {
            var path = Fixtures.CreateAnonymous<string>();
            var size = Fixtures.CreateAnonymous<int>();
            var provider = new TestableMultipartFileStreamProvider(path, size);

            Assert.That(provider.RootPath, Is.EqualTo(path));
        }

        [Test]
        public void Constructor2_SetsBufferSize()
        {
            var path = Fixtures.CreateAnonymous<string>();
            var size = Fixtures.CreateAnonymous<int>();
            var provider = new TestableMultipartFileStreamProvider(path, size);

            Assert.That(provider.BufferSize, Is.EqualTo(size));
        }

        [Test]
        public void Constructor3_SetsRootPath()
        {
            var path = Fixtures.CreateAnonymous<string>();
            var size = Fixtures.CreateAnonymous<int>();
            var provider = new TestableMultipartFileStreamProvider(path, size, null);

            Assert.That(provider.RootPath, Is.EqualTo(path));
        }

        [Test]
        public void Constructor3_SetsBufferSize()
        {
            var path = Fixtures.CreateAnonymous<string>();
            var size = Fixtures.CreateAnonymous<int>();
            var provider = new TestableMultipartFileStreamProvider(path, size, null);

            Assert.That(provider.BufferSize, Is.EqualTo(size));
        }

        [Test]
        public void GetStream_UsesPath()
        {
            var provider_factory = Fixtures.CreateAnonymous<FileStreamProviderFactory>();
            var content_factory = Fixtures.CreateAnonymous<MultipartFileContentFactory>();
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

        [Test]
        public void GetStream_CreatesNewFileName()
        {
            var provider_factory = Fixtures.CreateAnonymous<FileStreamProviderFactory>();
            var content_factory = Fixtures.CreateAnonymous<MultipartFileContentFactory>();
            var provider = provider_factory.NewProvider();

            var task = content_factory.NewContent().ReadAsMultipartAsync(provider).ContinueWith<HttpResponseMessage>(t =>
            {
                Assume.That(t.IsFaulted, Is.False);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
            task.Wait();

            var file = provider.FileData.First();
            Assert.That(file.LocalFileName, Is.Not.StringContaining(content_factory.SubPart1Filename));
        }

        [Test]
        public void GetLocalFileName_CreatesUniqueFilenames()
        {
            var provider_factory = Fixtures.CreateAnonymous<FileStreamProviderFactory>();
            var content_factory = Fixtures.CreateAnonymous<MultipartFileContentFactory>();
            var provider = provider_factory.NewProvider();

            var task = content_factory.NewContent().ReadAsMultipartAsync(provider).ContinueWith<HttpResponseMessage>(t =>
            {
                Assume.That(t.IsFaulted, Is.False);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
            task.Wait();

            var filenames = provider.FileData.Select(fd => fd.LocalFileName);
            Assert.That(filenames.Count(), Is.EqualTo(filenames.GroupBy(s => s).Count()));
        }

        [Test]
        public void GetStream_OpensStreamToCorrectPath()
        {
            var provider_factory = Fixtures.CreateAnonymous<FileStreamProviderFactory>();
            var content_factory = Fixtures.CreateAnonymous<MultipartFileContentFactory>();
            var provider = provider_factory.NewProvider();

            var task = content_factory.NewContent().ReadAsMultipartAsync(provider).ContinueWith<HttpResponseMessage>(t =>
            {
                Assume.That(t.IsFaulted, Is.False);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
            task.Wait();

            var expected_path = provider.FileData.First().LocalFileName;
            provider_factory.FileMock.Verify(f => f.Create(It.Is<string>(s => s == expected_path), It.IsAny<int>(), It.IsAny<FileOptions>()));
        }

        [Test]
        public void GetStream_OpensStreamWithCorrectBufferSize()
        {
            var provider_factory = Fixtures.CreateAnonymous<FileStreamProviderFactory>();
            var content_factory = Fixtures.CreateAnonymous<MultipartFileContentFactory>();
            var provider = provider_factory.NewProvider();

            var task = content_factory.NewContent().ReadAsMultipartAsync(provider).ContinueWith<HttpResponseMessage>(t =>
            {
                Assume.That(t.IsFaulted, Is.False);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
            task.Wait();

            provider_factory.FileMock.Verify(f => f.Create(It.IsAny<string>(), It.Is<int>(i => i == provider_factory.BufferSize), It.IsAny<FileOptions>()));
        }

        [Test]
        public void GetStream_OpensStreamWithAsyncOption()
        {
            var provider_factory = Fixtures.CreateAnonymous<FileStreamProviderFactory>();
            var content_factory = Fixtures.CreateAnonymous<MultipartFileContentFactory>();
            var provider = provider_factory.NewProvider();

            var task = content_factory.NewContent().ReadAsMultipartAsync(provider).ContinueWith<HttpResponseMessage>(t =>
            {
                Assume.That(t.IsFaulted, Is.False);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
            task.Wait();

            provider_factory.FileMock.Verify(f => f.Create(It.IsAny<string>(), It.IsAny<int>(), It.Is<FileOptions>(o => (o & FileOptions.Asynchronous) != 0)));
        }

        [Test]
        public void GetStream_AccessesTheStream()
        {
            var provider_factory = Fixtures.CreateAnonymous<FileStreamProviderFactory>();
            var content_factory = Fixtures.CreateAnonymous<MultipartFileContentFactory>();
            var provider = provider_factory.NewProvider();

            var task = content_factory.NewContent().ReadAsMultipartAsync(provider).ContinueWith<HttpResponseMessage>(t =>
            {
                Assume.That(t.IsFaulted, Is.False);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
            task.Wait();

            provider_factory.StreamMock.VerifyGet(fs => fs.StreamInstance);
        }

        class FileStreamProviderFactory
        {
            Fixture Fixtures = new Fixture();

            public string Path { get; set; }
            public int BufferSize { get; set; }
            public Mock<IFile> FileMock { get; set; }
            public Mock<IFileStream> StreamMock { get; set; }

            public TestableMultipartFileStreamProvider NewProvider()
            {
                FileMock.Setup(f => f.Create(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<FileOptions>())).Returns(StreamMock.Object);
                StreamMock.SetupGet(fs => fs.StreamInstance).Returns(new MemoryStream());
                return new TestableMultipartFileStreamProvider(Path, BufferSize, FileMock.Object);
            }
        }

        class MultipartFileContentFactory
        {
            Fixture Fixtures = new Fixture();

            public string SubPart1Name { get; set; }
            public string SubPart1Value { get; set; }
            public string SubPart1Filename { get; set; }
            public string SubPart2Name { get; set; }
            public string SubPart2Value { get; set; }
            public string SubPart2Filename { get; set; }

            public MultipartFormDataContent NewContent()
            {
                var content = new MultipartFormDataContent(Fixtures.CreateAnonymous<string>());
                var subpart1 = new ByteArrayContent(Encoding.UTF8.GetBytes(SubPart1Value));
                var subpart2 = new ByteArrayContent(Encoding.UTF8.GetBytes(SubPart2Value));
                content.Add(subpart1, SubPart1Name, SubPart1Filename);
                content.Add(subpart2, SubPart2Name, SubPart2Filename);
                return content;
            }
        }
    }
}
