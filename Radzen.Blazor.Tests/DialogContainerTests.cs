using Radzen.Blazor.Rendering;
using System.Threading.Tasks;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DialogContainerTests
    {
        public class DialogBeforeCloseTests
        {
            /// <summary>
            /// Mock implementation of the DialogService for testing purposes.
            /// </summary>
            public class DialogMockService : DialogService
            {
                /// <summary>
                /// Initializes a new instance of the <see cref="DialogMockService"/> class.
                /// </summary>
                public DialogMockService() : base(null, null)
                {
                }

                /// <summary>
                /// Gets or sets a value indicating whether the Close method has been called.
                /// </summary>
                public bool CloseHasBeenCalled { get; set; }

                /// <summary>
                /// Overrides the Close method to set the CloseHasBeenCalled flag to true.
                /// </summary>
                /// <param name="result">The result of the dialog.</param>
                public override void Close(dynamic result = null)
                {
                    CloseHasBeenCalled = true;
                }
            }

#pragma warning disable BL0005 // Component parameter should not be set outside of its component.

            [Fact(DisplayName = "CloseAsync should invoke OnBeforeClose")]
            public async Task CloseAsync_Should_Invoke_OnBeforeClose()
            {
                // Arrange
                var beforeCloseHasBeenCalled = false;

                // Create a mock dialog service
                using var dialogService = new DialogMockService();
                // Create a dialog container and set its service and dialog options
                using var dialogContainer = new DialogContainer()
                {
                    Service = dialogService,
                    Dialog = new()
                    {
                        Options = new DialogOptions
                        {
                            // Set the OnBeforeClose callback to set the flag to true
                            OnBeforeClose = () =>
                            {
                                beforeCloseHasBeenCalled = true;
                                return true;
                            }
                        }
                    }
                };

                // Act
                // Call CloseAsync on the dialog container
                await dialogContainer.CloseAsync();

                // Assert
                // Verify that the OnBeforeClose callback was called
                Assert.True(beforeCloseHasBeenCalled);
                // Verify that the Close method on the dialog service was called
                Assert.True(dialogService.CloseHasBeenCalled);
            }

            [Fact(DisplayName = "CloseAsync should invoke OnBeforeCloseAsync")]
            public async Task CloseAsync_Should_Invoke_OnBeforeCloseAsync()
            {
                // Arrange
                var beforeCloseHasBeenCalled = false;

                // Create a mock dialog service
                using var dialogService = new DialogMockService();
                // Create a dialog container and set its service and dialog options
                using var dialogContainer = new DialogContainer()
                {
                    Service = dialogService,
                    Dialog = new()
                    {
                        Options = new DialogOptions
                        {
                            // Set the OnBeforeCloseAsync callback to set the flag to true
                            OnBeforeCloseAsync = async () =>
                            {
                                beforeCloseHasBeenCalled = true;
                                await Task.CompletedTask;
                                return true;
                            }
                        }
                    }
                };

                // Act
                // Call CloseAsync on the dialog container
                await dialogContainer.CloseAsync();

                // Assert
                // Verify that the OnBeforeCloseAsync callback was called
                Assert.True(beforeCloseHasBeenCalled);
                // Verify that the Close method on the dialog service was called
                Assert.True(dialogService.CloseHasBeenCalled);
            }

            [Fact(DisplayName = "CloseAsync should not close if OnBeforeCloseAsync returns false")]
            public async Task CloseAsync_Should_Not_Close_If_OnBeforeCloseAsync_Returns_False()
            {
                // Arrange
                // Create a mock dialog service
                using var dialogService = new DialogMockService();
                // Create a dialog container and set its service and dialog options
                using var dialogContainer = new DialogContainer()
                {
                    Service = dialogService,
                    Dialog = new()
                    {
                        Options = new DialogOptions
                        {
                            // Set the OnBeforeCloseAsync callback to return false
                            OnBeforeCloseAsync = async () =>
                            {
                                await Task.CompletedTask;
                                return false;
                            }
                        }
                    }
                };

                // Act
                // Call CloseAsync on the dialog container
                await dialogContainer.CloseAsync();

                // Assert
                // Verify that the Close method on the dialog service was not called
                Assert.False(dialogService.CloseHasBeenCalled);
            }

            [Fact(DisplayName = "CloseAsync should not close if OnBeforeClose returns false")]
            public async Task CloseAsync_Should_Not_Close_If_OnBeforeClose_Returns_False()
            {
                // Arrange
                // Create a mock dialog service
                using var dialogService = new DialogMockService();
                // Create a dialog container and set its service and dialog options
                using var dialogContainer = new DialogContainer()
                {
                    Service = dialogService,
                    Dialog = new()
                    {
                        Options = new DialogOptions
                        {
                            // Set the OnBeforeClose callback to return false
                            OnBeforeClose = () => false
                        }
                    }
                };

                // Act
                // Call CloseAsync on the dialog container
                await dialogContainer.CloseAsync();

                // Assert
                // Verify that the Close method on the dialog service was not called
                Assert.False(dialogService.CloseHasBeenCalled);
            }

            [Fact(DisplayName = "CloseAsync should close if no OnBeforeClose or OnBeforeCloseAsync supplied")]
            public async Task CloseAsync_Should_Close_If_No_BeforeClose_Or_BeforeCloseAsync_Supplied()
            {
                // Arrange
                // Create a mock dialog service
                using var dialogService = new DialogMockService();
                // Create a dialog container and set its service and dialog options
                using var dialogContainer = new DialogContainer()
                {
                    Service = dialogService,
                    Dialog = new()
                    {
                        Options = new DialogOptions()
                    }
                };

                // Act
                // Call CloseAsync on the dialog container
                await dialogContainer.CloseAsync();

                // Assert
                // Verify that the Close method on the dialog service was called
                Assert.True(dialogService.CloseHasBeenCalled);
            }

#pragma warning restore BL0005 // Component parameter should not be set outside of its component.

        }
    }
}