using KoiFengShuiSystem.Modules.Community.Infrastructure.CloudStorage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;

namespace UnitTests.Community
{
    /// <summary>
    /// Pins the validation contract ported from the legacy CloudService:
    /// null, empty, and non-PNG/JPEG uploads are rejected with an exception
    /// before any provider call is made (provider I/O stays out of unit scope).
    /// </summary>
    public class CloudStorageServiceTests
    {
        private static CloudStorageService CreateService() => new(
            Options.Create(new CloudStorageSettings
            {
                CloudName = "test-cloud-name",
                ApiKey = "test-key",
                ApiSecret = "test-secret"
            }));

        [Theory]
        [InlineData("image/gif")]
        [InlineData("image/webp")]
        [InlineData("application/pdf")]
        public async Task UploadImageAsync_UnsupportedContentType_Throws(string contentType)
        {
            var service = CreateService();
            var file = CreateFormFile("avatar.png", contentType, 10);

            await Assert.ThrowsAsync<Exception>(() => service.UploadImageAsync(file));
        }

        [Fact]
        public async Task UploadImageAsync_EmptyFile_Throws()
        {
            var service = CreateService();
            var file = CreateFormFile("avatar.png", "image/png", length: 0);

            await Assert.ThrowsAsync<Exception>(() => service.UploadImageAsync(file));
        }

        [Fact]
        public async Task UploadImageAsync_NullFile_Throws()
        {
            var service = CreateService();

            await Assert.ThrowsAsync<Exception>(() => service.UploadImageAsync(null!));
        }

        private static IFormFile CreateFormFile(string fileName, string contentType, long length)
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.Length).Returns(length);
            fileMock.Setup(f => f.ContentType).Returns(contentType);
            return fileMock.Object;
        }
    }
}
