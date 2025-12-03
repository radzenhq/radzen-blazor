using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Radzen.Blazor.Tests
{
	public class DialogServiceTests : ComponentBase
	{
		public class OpenDialogTests
		{
			[Fact(DisplayName = "DialogOptions default values are set correctly")]
			public void DialogOptions_DefaultValues_AreSetCorrectly()
			{
				// Arrange
				var options = new DialogOptions();
				var dialogService = new DialogService(null, null);

				// Act
				dialogService.OpenDialog<DialogServiceTests>("Test", [], options);

				// Assert
				Assert.Equal("600px", options.Width);
				Assert.Equal("", options.Left);
				Assert.Equal("", options.Top);
				Assert.Equal("", options.Bottom);
				Assert.Equal("", options.Height);
				Assert.Equal("", options.Style);
				Assert.Equal("", options.CssClass);
				Assert.Equal("", options.WrapperCssClass);
				Assert.Equal("", options.ContentCssClass);
			}

			[Fact(DisplayName = "DialogOptions values are retained after OpenDialog call")]
			public void DialogOptions_Values_AreRetained_AfterOpenDialogCall()
			{
				// Arrange
				var options = new DialogOptions
				{
					Width = "800px",
					Left = "10px",
					Top = "20px",
					Bottom = "30px",
					Height = "400px",
					Style = "background-color: red;",
					CssClass = "custom-class",
					WrapperCssClass = "wrapper-class",
					ContentCssClass = "content-class"
				};
				var dialogService = new DialogService(null, null);

				// Act
				dialogService.OpenDialog<DialogServiceTests>("Test", [], options);

				// Assert
				Assert.Equal("800px", options.Width);
				Assert.Equal("10px", options.Left);
				Assert.Equal("20px", options.Top);
				Assert.Equal("30px", options.Bottom);
				Assert.Equal("400px", options.Height);
				Assert.Equal("background-color: red;", options.Style);
				Assert.Equal("custom-class", options.CssClass);
				Assert.Equal("wrapper-class", options.WrapperCssClass);
				Assert.Equal("content-class", options.ContentCssClass);
			}

			[Fact(DisplayName = "DialogOptions is null and default values are set correctly")]
			public void DialogOptions_IsNull_DefaultValues_AreSetCorrectly()
			{
				// Arrange
				DialogOptions resultingOptions = null;
				var dialogService = new DialogService(null, null);
				dialogService.OnOpen += (title, type, parameters, options) => resultingOptions = options;

				// Act
				dialogService.OpenDialog<DialogServiceTests>("Test", [], null);

				// Assert
				Assert.NotNull(resultingOptions);
				Assert.Equal("600px", resultingOptions.Width);
				Assert.Equal("", resultingOptions.Left);
				Assert.Equal("", resultingOptions.Top);
				Assert.Equal("", resultingOptions.Bottom);
				Assert.Equal("", resultingOptions.Height);
				Assert.Equal("", resultingOptions.Style);
				Assert.Equal("", resultingOptions.CssClass);
				Assert.Equal("", resultingOptions.WrapperCssClass);
				Assert.Equal("", resultingOptions.ContentCssClass);
			}

            [Fact(DisplayName = "Open with dynamic component type reflective calls are resolved without exception")]
            public void Open_DynamicComponentType_Reflective_Calls_Resolve()
            {
                // Arrange
                string resultingTitle = null;
                Type resultingType = null;
                var dialogService = new DialogService(null, null);
                dialogService.OnOpen += (title, type, _, _) =>
                {
                    resultingTitle = title;
                    resultingType = type;
                };
                
                dialogService.Open("Dynamic Open", typeof(RadzenButton), []);
                
                // Assert
                Assert.Equal("Dynamic Open", resultingTitle);
                Assert.Equal(typeof(RadzenButton), resultingType);
            }
            
            [Fact(DisplayName = "OpenAsync with dynamic component type reflective calls are resolved without exception")]
            public async Task OpenAsync_DynamicComponentType_Reflective_Calls_Resolve()
            {
                // Arrange
                string resultingTitle = null;
                Type resultingType = null;
                var dialogService = new DialogService(null, null);
                dialogService.OnOpen += (title, type, _, _) =>
                {
                    resultingTitle = title;
                    resultingType = type;
                };

                var openTask = dialogService.OpenAsync("Dynamic Open", typeof(RadzenButton), []);
                dialogService.Close();
                await openTask;

                // Assert
                Assert.Equal("Dynamic Open", resultingTitle);
                Assert.Equal(typeof(RadzenButton), resultingType);
            }
		}

        public class OpenSideDialogTests
        {
            [Fact(DisplayName = "SideDialogOptions resizable option is retained after OpenSideDialog call")]
            public void SideDialogOptions_Resizable_AreRetained_AfterOpenSideDialogCall()
            {
                // Arrange
                var options = new SideDialogOptions { Resizable = true };
                SideDialogOptions resultingOptions = null;
                var dialogService = new DialogService(null, null);
                dialogService.OnSideOpen += (_, _, sideOptions) => resultingOptions = sideOptions;

                // Act
                dialogService.OpenSide<DialogServiceTests>("Test", [], options);

                // Assert
                Assert.NotNull(resultingOptions);
                Assert.Same(options, resultingOptions);
                Assert.True(resultingOptions.Resizable);
            }

            [Fact(DisplayName = "Side dialog shows resize bar when Resizable is true")]
            public void SideDialog_Resizable_ShowsResizeBar()
            {
                using var ctx = new TestContext();
                ctx.JSInterop.Mode = JSRuntimeMode.Loose;
                ctx.Services.AddScoped<DialogService>();

                // Render the dialog host
                var cut = ctx.RenderComponent<RadzenDialog>();

                // Open a side dialog with Resizable=true
                var dialogService = ctx.Services.GetRequiredService<DialogService>();
                cut.InvokeAsync(() => dialogService.OpenSide("Test", typeof(RadzenButton),
                    new Dictionary<string, object>(), new SideDialogOptions { Resizable = true }));

                // Assert: the resize bar element is present
                cut.WaitForAssertion(() =>
                {
                    var markup = cut.Markup;
                    Assert.Contains("rz-dialog-resize-bar", markup);
                    // Optionally ensure the inner handle exists too
                    Assert.Contains("rz-resize", markup);
                });
            }

            [Fact(DisplayName = "Side dialog hides resize bar when Resizable is false")]
            public void SideDialog_NonResizable_HidesResizeBar()
            {
                using var ctx = new TestContext();
                ctx.JSInterop.Mode = JSRuntimeMode.Loose;
                ctx.Services.AddScoped<DialogService>();

                // Render the dialog host
                var cut = ctx.RenderComponent<RadzenDialog>();

                // Open a side dialog with Resizable=false
                var dialogService = ctx.Services.GetRequiredService<DialogService>();
                cut.InvokeAsync(() => dialogService.OpenSide("Test", typeof(RadzenButton),
                    new Dictionary<string, object>(), new SideDialogOptions()));

                // Assert: the resize bar element is not present
                cut.WaitForAssertion(() =>
                {
                    var markup = cut.Markup;
                    Assert.DoesNotContain("rz-dialog-resize-bar", markup);
                });
            }
        }
		public class ConfirmTests
		{
			[Fact(DisplayName = "ConfirmOptions is null and default values are set correctly")]
			public async Task ConfirmOptions_IsNull_AreSetCorrectly()
			{
				// Arrange
				var dialogService = new DialogService(null, null);
				ConfirmOptions resultingOptions = null;
				dialogService.OnOpen += (title, type, parameters, options) => resultingOptions = options as ConfirmOptions;

				using var cancellationTokenSource = new CancellationTokenSource();
				cancellationTokenSource.Cancel();

				// Act
				try
				{
					await dialogService.Confirm(cancellationToken: cancellationTokenSource.Token);
				}
				catch (TaskCanceledException)
				{
					// this is expected
				}

				// Assert
				Assert.NotNull(resultingOptions);
                Assert.Equal("Ok", resultingOptions.OkButtonText);
                Assert.Equal("Cancel", resultingOptions.CancelButtonText);
                Assert.Equal("600px", resultingOptions.Width);
				Assert.Equal("", resultingOptions.Style);
				Assert.Equal("rz-dialog-confirm", resultingOptions.CssClass);
				Assert.Equal("rz-dialog-wrapper", resultingOptions.WrapperCssClass);
			}

			[Fact(DisplayName = "ConfirmOptions default values are set correctly")]
			public async Task ConfirmOptions_DefaultValues_AreSetCorrectly()
			{
				// Arrange
				var dialogService = new DialogService(null, null);
				ConfirmOptions resultingOptions = null;
				dialogService.OnOpen += (title, type, parameters, options) => resultingOptions = options as ConfirmOptions;

				using var cancellationTokenSource = new CancellationTokenSource();
				cancellationTokenSource.Cancel();

				// Act
				try
				{
					await dialogService.Confirm(options: new(), cancellationToken: cancellationTokenSource.Token);
				}
				catch (TaskCanceledException)
				{
					// this is expected
				}

				// Assert
				Assert.NotNull(resultingOptions);
                Assert.Equal("Ok", resultingOptions.OkButtonText);
                Assert.Equal("Cancel", resultingOptions.CancelButtonText);
                Assert.Equal("600px", resultingOptions.Width);
				Assert.Equal("", resultingOptions.Style);
				Assert.Equal("rz-dialog-confirm", resultingOptions.CssClass);
				Assert.Equal("rz-dialog-wrapper", resultingOptions.WrapperCssClass);
			}
			[Fact(DisplayName = "ConfirmOptions values are retained after Confirm call")]
			public async Task Confirm_ProvidedValues_AreRetained()
			{
				// Arrange
				var dialogService = new DialogService(null, null);
				var options = new ConfirmOptions
				{
                    OkButtonText = "XXX",
                    CancelButtonText = "YYY",
                    Width = "800px",
					Style = "background-color: red;",
					CssClass = "custom-class",
					WrapperCssClass = "wrapper-class"
				};
				ConfirmOptions resultingOptions = null;
				dialogService.OnOpen += (title, type, parameters, options) => resultingOptions = options as ConfirmOptions;

				// We break out of the dialog immediately, but the options should still be set
				using var cancellationTokenSource = new CancellationTokenSource();
				cancellationTokenSource.Cancel();

				// Act
				try
				{
					await dialogService.Confirm("Confirm?", "Confirm", options, cancellationToken: cancellationTokenSource.Token);
				}
				catch (TaskCanceledException)
				{
					// this is expected
				}

				// Assert
				Assert.NotNull(resultingOptions);
                Assert.Equal("XXX", resultingOptions.OkButtonText);
                Assert.Equal("YYY", resultingOptions.CancelButtonText);
                Assert.Equal("800px", resultingOptions.Width);
				Assert.Equal("background-color: red;", resultingOptions.Style);
				Assert.Equal("rz-dialog-confirm custom-class", resultingOptions.CssClass);
				Assert.Equal("rz-dialog-wrapper wrapper-class", resultingOptions.WrapperCssClass);
			}

		}

		public class AlertTests
		{
			[Fact(DisplayName = "AlertOptions is null and default values are set correctly")]
			public async Task AlertOptions_IsNull_AreSetCorrectly()
			{
				// Arrange
				var dialogService = new DialogService(null, null);
				AlertOptions resultingOptions = null;
				dialogService.OnOpen += (title, type, parameters, options) => resultingOptions = options as AlertOptions;

				using var cancellationTokenSource = new CancellationTokenSource();
				cancellationTokenSource.Cancel();

				// Act
				try
				{
					await dialogService.Alert(cancellationToken: cancellationTokenSource.Token);
				}
				catch (TaskCanceledException)
				{
					// this is expected
				}

				// Assert
				Assert.NotNull(resultingOptions);
                Assert.Equal("Ok", resultingOptions.OkButtonText);
				Assert.Equal("600px", resultingOptions.Width);
				Assert.Equal("", resultingOptions.Style);
				Assert.Equal("rz-dialog-alert", resultingOptions.CssClass);
				Assert.Equal("rz-dialog-wrapper", resultingOptions.WrapperCssClass);
			}

			[Fact(DisplayName = "AlertOptions default values are set correctly")]
			public async Task AlertOptions_DefaultValues_AreSetCorrectly()
			{
				// Arrange
				var dialogService = new DialogService(null, null);
				AlertOptions resultingOptions = null;
				dialogService.OnOpen += (title, type, parameters, options) => resultingOptions = options as AlertOptions;

				using var cancellationTokenSource = new CancellationTokenSource();
				cancellationTokenSource.Cancel();

				// Act
				try
				{
					await dialogService.Alert(options: new(), cancellationToken: cancellationTokenSource.Token);
				}
				catch (TaskCanceledException)
				{
					// this is expected
				}

				// Assert
				Assert.NotNull(resultingOptions);
                Assert.Equal("Ok", resultingOptions.OkButtonText);
				Assert.Equal("600px", resultingOptions.Width);
				Assert.Equal("", resultingOptions.Style);
				Assert.Equal("rz-dialog-alert", resultingOptions.CssClass);
				Assert.Equal("rz-dialog-wrapper", resultingOptions.WrapperCssClass);
			}
			[Fact(DisplayName = "AlertOptions values are retained after Alert call")]
			public async Task Alert_ProvidedValues_AreRetained()
			{
				// Arrange
				var dialogService = new DialogService(null, null);
				var options = new AlertOptions
				{
                    OkButtonText = "XXX",
					Width = "800px",
					Style = "background-color: red;",
					CssClass = "custom-class",
					WrapperCssClass = "wrapper-class"
				};
				AlertOptions resultingOptions = null;
				dialogService.OnOpen += (title, type, parameters, options) => resultingOptions = options as AlertOptions;

				// We break out of the dialog immediately, but the options should still be set
				using var cancellationTokenSource = new CancellationTokenSource();
				cancellationTokenSource.Cancel();

				// Act
				try
				{
					await dialogService.Alert("Alert?", "Alert", options, cancellationToken: cancellationTokenSource.Token);
				}
				catch (TaskCanceledException)
				{
					// this is expected
				}

				// Assert
				Assert.NotNull(resultingOptions);
				Assert.Equal("XXX", resultingOptions.OkButtonText);
				Assert.Equal("800px", resultingOptions.Width);
				Assert.Equal("background-color: red;", resultingOptions.Style);
				Assert.Equal("rz-dialog-alert custom-class", resultingOptions.CssClass);
				Assert.Equal("rz-dialog-wrapper wrapper-class", resultingOptions.WrapperCssClass);
			}
		}
	}
}