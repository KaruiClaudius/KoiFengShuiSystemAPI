using System.Text.Json;
using KoiFengShuiSystem.Host.Middleware;

namespace UnitTests.Host
{
    public class ExceptionProblemMapperTests
    {
        [Theory]
        [InlineData(typeof(ArgumentException), 400)]
        [InlineData(typeof(ArgumentNullException), 400)]
        [InlineData(typeof(KeyNotFoundException), 404)]
        [InlineData(typeof(InvalidOperationException), 409)]
        [InlineData(typeof(UnauthorizedAccessException), 403)]
        [InlineData(typeof(ApplicationException), 400)]
        [InlineData(typeof(IOException), 500)]
        public void ResolveStatus_MapsExceptionFamilies(Type exceptionType, int expectedStatus)
        {
            var exception = (Exception)Activator.CreateInstance(exceptionType, "boom")!;

            Assert.Equal(expectedStatus, ExceptionProblemMapper.ResolveStatus(exception));
        }

        [Fact]
        public void ResolveStatus_ClientFaults_NeverLeakViaServerTitle()
        {
            Assert.NotEqual("An unexpected error occurred", ExceptionProblemMapper.ResolveTitle(400));
            Assert.Equal("An unexpected error occurred", ExceptionProblemMapper.ResolveTitle(500));
        }
    }
}
