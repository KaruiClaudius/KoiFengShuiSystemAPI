using KoiFengShuiSystem.Common;
using KoiFengShuiSystem.Modules.Community.Api.Controllers;
using KoiFengShuiSystem.Modules.Community.Application.Requests;
using KoiFengShuiSystem.Modules.Community.Application.Services;
using KoiFengShuiSystem.Shared.Kernel.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using UnitTests.Api;

namespace UnitTests.Community
{
    /// <summary>
    /// Ports the legacy PostControllerTests onto the module-owned controller
    /// contract, keeping every asserted status-code behavior identical.
    /// </summary>
    public class PostModuleControllerTests
    {
        private readonly Mock<IPostService> _postServiceMock = new();
        private readonly Mock<ILogger<PostController>> _loggerMock = new();

        private PostController CreateController() => new(_postServiceMock.Object, _loggerMock.Object);

        [Fact]
        public async Task CreateAsync_ServiceReportsSuccess_ReturnsOk()
        {
            _postServiceMock
                .Setup(s => s.CreatePost(It.IsAny<CreatePostRequest>(), It.IsAny<int>()))
                .ReturnsAsync(new BusinessResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG));

            var controller = CreateController();

            var result = await controller.CreateAsync(ValidRequest());

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task CreateAsync_ServiceReportsFailure_ReturnsBadRequest()
        {
            _postServiceMock
                .Setup(s => s.CreatePost(It.IsAny<CreatePostRequest>(), It.IsAny<int>()))
                .ReturnsAsync(new BusinessResult(Const.FAIL_CREATE_CODE, Const.FAIL_CREATE_MSG));

            var controller = CreateController();

            var result = await controller.CreateAsync(ValidRequest());

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateAsync_PassesAuthenticatedAccountIdAsAuthor()
        {
            _postServiceMock
                .Setup(s => s.CreatePost(It.IsAny<CreatePostRequest>(), It.IsAny<int>()))
                .ReturnsAsync(new BusinessResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG));

            var controller = CreateController();
            TestClaimsPrincipalFactory.AttachAccountId(controller, 42);

            await controller.CreateAsync(ValidRequest());

            _postServiceMock.Verify(s => s.CreatePost(It.IsAny<CreatePostRequest>(), 42), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_MissingAccountIdClaim_StillCallsServiceWithZero()
        {
            _postServiceMock
                .Setup(s => s.CreatePost(It.IsAny<CreatePostRequest>(), It.IsAny<int>()))
                .ReturnsAsync(new BusinessResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG));

            var controller = CreateController();

            await controller.CreateAsync(ValidRequest());

            _postServiceMock.Verify(s => s.CreatePost(It.IsAny<CreatePostRequest>(), 0), Times.Once);
        }

        private static CreatePostRequest ValidRequest() => new()
        {
            Title = "Koi pond basics",
            Content = "How to keep koi healthy",
            CategoryId = 1
        };
    }
}
