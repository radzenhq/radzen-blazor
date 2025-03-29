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

            [Fact(DisplayName = "CloseAsync should invoke OnBeforeDialogClose")]
            public async Task CloseAsync_Should_Invoke_OnBeforeDialogClose()
            {
                // Arrange
                var beforeDialogCloseHasBeenCalled = false;

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
                            // Set the OnBeforeDialogClose callback to set the flag to true
                            OnBeforeDialogClose = dialog =>
                            {
                                beforeDialogCloseHasBeenCalled = true;
                                return true;
                            }
                        }
                    }
                };

                // Act
                // Call CloseAsync on the dialog container
                await dialogContainer.CloseAsync();

                // Assert
                // Verify that the OnBeforeDialogClose callback was called
                Assert.True(beforeDialogCloseHasBeenCalled);
                // Verify that the Close method on the dialog service was called
                Assert.True(dialogService.CloseHasBeenCalled);
            }

            [Fact(DisplayName = "CloseAsync should invoke OnBeforeDialogCloseAsync")]
            public async Task CloseAsync_Should_Invoke_OnBeforeDialogCloseAsync()
            {
                // Arrange
                var beforeDialogCloseHasBeenCalled = false;

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
                            // Set the OnBeforeDialogCloseAsync callback to set the flag to true
                            OnBeforeDialogCloseAsync = async dialog =>
                            {
                                beforeDialogCloseHasBeenCalled = true;
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
                // Verify that the OnBeforeDialogCloseAsync callback was called
                Assert.True(beforeDialogCloseHasBeenCalled);
                // Verify that the Close method on the dialog service was called
                Assert.True(dialogService.CloseHasBeenCalled);
            }

            [Fact(DisplayName = "CloseAsync should not close if OnBeforeDialogCloseAsync returns false")]
            public async Task CloseAsync_Should_Not_Close_If_OnBeforeDialogCloseAsync_Returns_False()
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
                            // Set the OnBeforeDialogCloseAsync callback to return false
                            OnBeforeDialogCloseAsync = async dialog =>
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

            [Fact(DisplayName = "CloseAsync should not close if OnBeforeDialogClose returns false")]
            public async Task CloseAsync_Should_Not_Close_If_OnBeforeDialogClose_Returns_False()
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
                            // Set the OnBeforeDialogClose callback to return false
                            OnBeforeDialogClose = dialog => false
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

            [Fact(DisplayName = "CloseAsync should close if no OnBeforeDialogClose or OnBeforeDialogCloseAsync supplied")]
            public async Task CloseAsync_Should_Close_If_No_BeforeDialogClose_Or_BeforeDialogCloseAsync_Supplied()
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