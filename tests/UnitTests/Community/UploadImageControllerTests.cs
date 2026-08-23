using KoiFengShuiSystem.Shared.Kernel;
using KoiFengShuiSystem.Modules.Community.Api.Controllers;
using KoiFengShuiSystem.Modules.Community.Application.Abstractions;
using KoiFengShuiSystem.Modules.Community.Application.Requests;
using KoiFengShuiSystem.Modules.Community.Application.Responses;
using KoiFengShuiSystem.Shared.Kernel.Results;
using Microsoft.AspNetCore.Http;
using Moq;

namespace UnitTests.Community
{
    /// <summary>
    /// Ports the legacy UploadImageController envelope contract onto the
    /// module-owned controller: identical IBusinessResult statuses, messages,
    /// and Data payload for every outcome.
    /// </summary>
    public class UploadImageControllerTests
    {
        private readonly Mock<ICloudStorage> _cloudStorageMock = new();
        private readonly Mock<ICommunityStore> _storeMock = new();

        private UploadImageController CreateController() =>
            new(_cloudStorageMock.Object, _storeMock.Object);

        [Fact]
        public async Task UploadFile_MissingFile_ReturnsFailEnvelope_WithoutCloudCall()
        {
            var controller = CreateController();

            var result = await controller.UploadFile(new UploadFileRequest());

            Assert.Equal(ResponseCodes.FailCreateCode, result.Status);
            Assert.Equal(ResponseCodes.FailCreateMessage, result.Message);
            Assert.Null(result.Data);
            _cloudStorageMock.Verify(c => c.UploadImageAsync(It.IsAny<IFormFile>()), Times.Never);
        }

        [Fact]
        public async Task UploadFile_EmptyFile_ReturnsFailEnvelope()
        {
            var controller = CreateController();

            var result = await controller.UploadFile(new UploadFileRequest
            {
                File = CreateFormFile("avatar.png", "image/png", length: 0)
            });

            Assert.Equal(ResponseCodes.FailCreateCode, result.Status);
            _cloudStorageMock.Verify(c => c.UploadImageAsync(It.IsAny<IFormFile>()), Times.Never);
        }

        [Fact]
        public async Task UploadFile_ProviderRejectsImage_ReturnsFailEnvelopeWithProviderMessage()
        {
            _cloudStorageMock
                .Setup(c => c.UploadImageAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync(new CloudUploadResult(false, null, "Image invalid"));

            var controller = CreateController();

            var result = await controller.UploadFile(ValidRequest());

            Assert.Equal(ResponseCodes.FailCreateCode, result.Status);
            Assert.Equal("Upload file error: Image invalid", result.Message);
        }

        [Fact]
        public async Task UploadFile_Success_PersistsUrl_AndReturnsEnvelopeWithData()
        {
            const string url = "https://res.cloudinary.com/demo/koi.png";
            _cloudStorageMock
                .Setup(c => c.UploadImageAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync(new CloudUploadResult(true, url, null));
            _storeMock
                .Setup(s => s.AddImageAsync(url))
                .ReturnsAsync(true);

            var controller = CreateController();

            var result = await controller.UploadFile(ValidRequest());

            Assert.Equal(ResponseCodes.SuccessCreateCode, result.Status);
            Assert.Equal(ResponseCodes.SuccessCreateMessage, result.Message);
            var data = Assert.IsType<UploadImageResponse>(result.Data);
            Assert.Equal(url, data.Url);
        }

        [Fact]
        public async Task UploadFile_StoreReportsFailure_ReturnsFailEnvelope()
        {
            const string url = "https://res.cloudinary.com/demo/koi.png";
            _cloudStorageMock
                .Setup(c => c.UploadImageAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync(new CloudUploadResult(true, url, null));
            _storeMock
                .Setup(s => s.AddImageAsync(url))
                .ReturnsAsync(false);

            var controller = CreateController();

            var result = await controller.UploadFile(ValidRequest());

            Assert.Equal(ResponseCodes.FailCreateCode, result.Status);
            Assert.Equal(ResponseCodes.FailCreateMessage, result.Message);
        }

        [Fact]
        public async Task UploadFile_CloudStorageThrows_ReturnsFailEnvelope()
        {
            _cloudStorageMock
                .Setup(c => c.UploadImageAsync(It.IsAny<IFormFile>()))
                .ThrowsAsync(new Exception("Failed to upload image to Cloudinary."));

            var controller = CreateController();

            var result = await controller.UploadFile(ValidRequest());

            Assert.Equal(ResponseCodes.FailCreateCode, result.Status);
            Assert.Equal(ResponseCodes.FailCreateMessage, result.Message);
        }

        private static UploadFileRequest ValidRequest() => new()
        {
            File = CreateFormFile("avatar.png", "image/png", length: 16)
        };

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
