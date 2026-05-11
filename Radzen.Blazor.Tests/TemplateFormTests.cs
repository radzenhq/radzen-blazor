using System.ComponentModel.DataAnnotations;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class TemplateFormTests
    {
        class ContactInfo
        {
            [Required]
            public string Name { get; set; }
        }

        [Fact]
        public void Submit_Passes_EditContext_Model_When_Data_Not_Set()
        {
            using var ctx = new TestContext();
            var model = new ContactInfo { Name = "Alice" };
            var editContext = new EditContext(model);

            ContactInfo submitted = null;

            var form = ctx.RenderComponent<RadzenTemplateForm<ContactInfo>>(parameters => parameters
                .Add(p => p.EditContext, editContext)
                .Add(p => p.Submit, EventCallback.Factory.Create<ContactInfo>(this, value => submitted = value)));

            form.Find("form").Submit();

            Assert.Same(model, submitted);
        }
    }
}
