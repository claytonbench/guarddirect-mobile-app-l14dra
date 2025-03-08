using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Xunit;
using Moq;
using FluentAssertions;
using SecurityPatrol.MAUI.UnitTests.Setup;
using SecurityPatrol.Services;
using SecurityPatrol.Constants;

namespace SecurityPatrol.MAUI.UnitTests.Services
{
    /// <summary>
    /// Test class for the NavigationService implementation that handles navigation in the Security Patrol application
    /// </summary>
    public class NavigationServiceTests
    {
        private NavigationService navigationService;
        private Mock<Shell> mockShell;

        /// <summary>
        /// Initializes a new instance of the NavigationServiceTests class with test setup
        /// </summary>
        public NavigationServiceTests()
        {
            // Initialize mock shell
            mockShell = new Mock<Shell>();
            
            // Setup Shell.Current using reflection
            var shellType = typeof(Shell);
            var currentProperty = shellType.GetProperty("Current");
            if (currentProperty != null && currentProperty.CanWrite)
            {
                currentProperty.SetValue(null, mockShell.Object);
            }
            
            // Initialize navigation service
            navigationService = new NavigationService();
        }

        /// <summary>
        /// Tests that NavigateToAsync correctly navigates to a valid route
        /// </summary>
        [Fact]
        public async Task Test_NavigateToAsync_ValidRoute()
        {
            // Arrange
            mockShell.Setup(s => s.GoToAsync(NavigationConstants.TimeTrackingPage, true))
                .Returns(Task.CompletedTask);

            // Act
            await navigationService.NavigateToAsync(NavigationConstants.TimeTrackingPage);

            // Assert
            mockShell.Verify(s => s.GoToAsync(NavigationConstants.TimeTrackingPage, true), Times.Once());
        }

        /// <summary>
        /// Tests that NavigateToAsync correctly includes parameters in the navigation
        /// </summary>
        [Fact]
        public async Task Test_NavigateToAsync_WithParameters()
        {
            // Arrange
            var parameters = new Dictionary<string, object>
            {
                { "testParam", "testValue" },
                { "numParam", 123 }
            };
            mockShell.Setup(s => s.GoToAsync(It.IsAny<string>(), true))
                .Returns(Task.CompletedTask);

            // Act
            await navigationService.NavigateToAsync(NavigationConstants.PatrolPage, parameters);

            // Assert
            mockShell.Verify(s => s.GoToAsync(It.Is<string>(route => 
                route.StartsWith(NavigationConstants.PatrolPage) && 
                route.Contains("testParam=testValue") && 
                route.Contains("numParam=123")), true), Times.Once());
        }

        /// <summary>
        /// Tests that NavigateToAsync throws ArgumentException for null route
        /// </summary>
        [Fact]
        public async Task Test_NavigateToAsync_NullRoute()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                navigationService.NavigateToAsync(null));
        }

        /// <summary>
        /// Tests that NavigateToAsync throws ArgumentException for empty route
        /// </summary>
        [Fact]
        public async Task Test_NavigateToAsync_EmptyRoute()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                navigationService.NavigateToAsync(""));
        }

        /// <summary>
        /// Tests that NavigateBackAsync correctly navigates back
        /// </summary>
        [Fact]
        public async Task Test_NavigateBackAsync()
        {
            // Arrange
            mockShell.Setup(s => s.GoToAsync("..", true))
                .Returns(Task.CompletedTask);

            // Act
            await navigationService.NavigateBackAsync();

            // Assert
            mockShell.Verify(s => s.GoToAsync("..", true), Times.Once());
        }

        /// <summary>
        /// Tests that NavigateToRootAsync correctly navigates to the root page
        /// </summary>
        [Fact]
        public async Task Test_NavigateToRootAsync()
        {
            // Arrange
            mockShell.Setup(s => s.GoToAsync("//", true))
                .Returns(Task.CompletedTask);

            // Act
            await navigationService.NavigateToRootAsync();

            // Assert
            mockShell.Verify(s => s.GoToAsync("//", true), Times.Once());
        }

        /// <summary>
        /// Tests that NavigateToModalAsync correctly opens a modal page
        /// </summary>
        [Fact]
        public async Task Test_NavigateToModalAsync_ValidRoute()
        {
            // Arrange
            mockShell.Setup(s => s.GoToAsync(NavigationConstants.PhotoCapturePage, true))
                .Returns(Task.CompletedTask);

            // Act
            await navigationService.NavigateToModalAsync(NavigationConstants.PhotoCapturePage);

            // Assert
            mockShell.Verify(s => s.GoToAsync(NavigationConstants.PhotoCapturePage, true), Times.Once());
        }

        /// <summary>
        /// Tests that NavigateToModalAsync correctly includes parameters in the modal navigation
        /// </summary>
        [Fact]
        public async Task Test_NavigateToModalAsync_WithParameters()
        {
            // Arrange
            var parameters = new Dictionary<string, object>
            {
                { "testParam", "testValue" },
                { "numParam", 123 }
            };
            mockShell.Setup(s => s.GoToAsync(It.IsAny<string>(), true))
                .Returns(Task.CompletedTask);

            // Act
            await navigationService.NavigateToModalAsync(NavigationConstants.ReportPage, parameters);

            // Assert
            mockShell.Verify(s => s.GoToAsync(It.Is<string>(route => 
                route.StartsWith(NavigationConstants.ReportPage) && 
                route.Contains("testParam=testValue") && 
                route.Contains("numParam=123")), true), Times.Once());
        }

        /// <summary>
        /// Tests that CloseModalAsync correctly closes the current modal
        /// </summary>
        [Fact]
        public async Task Test_CloseModalAsync()
        {
            // Arrange
            mockShell.Setup(s => s.GoToAsync("..", true))
                .Returns(Task.CompletedTask);

            // Act
            await navigationService.CloseModalAsync();

            // Assert
            mockShell.Verify(s => s.GoToAsync("..", true), Times.Once());
        }

        /// <summary>
        /// Tests that GetCurrentRoute correctly returns the current route
        /// </summary>
        [Fact]
        public void Test_GetCurrentRoute()
        {
            // Arrange
            var testUri = new Uri("//MainPage", UriKind.Relative);
            var shellState = new ShellNavigationState(testUri);
            mockShell.Setup(s => s.CurrentState).Returns(shellState);

            // Act
            string result = navigationService.GetCurrentRoute();

            // Assert
            result.Should().Be("//MainPage");
        }

        /// <summary>
        /// Tests that GetRouteParameter correctly retrieves an existing parameter
        /// </summary>
        [Fact]
        public void Test_GetRouteParameter_ExistingParameter()
        {
            // Arrange
            var testUri = new Uri("//MainPage?testParam=testValue", UriKind.Relative);
            var shellState = new ShellNavigationState(testUri);
            mockShell.Setup(s => s.CurrentState).Returns(shellState);

            // Act
            object result = navigationService.GetRouteParameter("testParam");

            // Assert
            result.Should().Be("testValue");
        }

        /// <summary>
        /// Tests that GetRouteParameter returns null for non-existing parameter
        /// </summary>
        [Fact]
        public void Test_GetRouteParameter_NonExistingParameter()
        {
            // Arrange
            var testUri = new Uri("//MainPage?otherParam=value", UriKind.Relative);
            var shellState = new ShellNavigationState(testUri);
            mockShell.Setup(s => s.CurrentState).Returns(shellState);

            // Act
            object result = navigationService.GetRouteParameter("nonExistingParam");

            // Assert
            result.Should().BeNull();
        }

        /// <summary>
        /// Tests that GetRouteParameter throws ArgumentException for null parameter name
        /// </summary>
        [Fact]
        public void Test_GetRouteParameter_NullParameterName()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => navigationService.GetRouteParameter(null));
        }

        /// <summary>
        /// Tests that BuildQueryString correctly formats query parameters
        /// </summary>
        [Fact]
        public void Test_BuildQueryString_WithParameters()
        {
            // Arrange
            var parameters = new Dictionary<string, object>
            {
                { "testParam", "testValue" },
                { "numParam", 123 }
            };
            
            // Use reflection to access private BuildQueryString method
            var method = typeof(NavigationService).GetMethod("BuildQueryString", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            // Act
            string result = (string)method.Invoke(navigationService, new object[] { parameters });

            // Assert
            result.Should().StartWith("?");
            result.Should().Contain("testParam=testValue");
            result.Should().Contain("numParam=123");
        }

        /// <summary>
        /// Tests that BuildQueryString returns empty string for empty parameters
        /// </summary>
        [Fact]
        public void Test_BuildQueryString_EmptyParameters()
        {
            // Arrange
            var emptyParameters = new Dictionary<string, object>();
            
            // Use reflection to access private BuildQueryString method
            var method = typeof(NavigationService).GetMethod("BuildQueryString", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            // Act
            string result = (string)method.Invoke(navigationService, new object[] { emptyParameters });

            // Assert
            result.Should().BeEmpty();
        }

        /// <summary>
        /// Tests that BuildQueryString returns empty string for null parameters
        /// </summary>
        [Fact]
        public void Test_BuildQueryString_NullParameters()
        {
            // Arrange
            Dictionary<string, object> nullParameters = null;
            
            // Use reflection to access private BuildQueryString method
            var method = typeof(NavigationService).GetMethod("BuildQueryString", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            // Act
            string result = (string)method.Invoke(navigationService, new object[] { nullParameters });

            // Assert
            result.Should().BeEmpty();
        }

        /// <summary>
        /// Tests that NavigationService methods handle Shell exceptions gracefully
        /// </summary>
        [Fact]
        public async Task Test_NavigationService_ShellException()
        {
            // Arrange
            mockShell.Setup(s => s.GoToAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .ThrowsAsync(new Exception("Shell navigation error"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                navigationService.NavigateToAsync(NavigationConstants.MainPage));
        }
    }
}