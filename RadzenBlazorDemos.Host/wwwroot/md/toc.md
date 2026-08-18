# Toc

The Blazor ToC auto-generates a table of contents from the headings on the current page.

Keywords: toc, content, navigation

> API reference: [RadzenToc API](https://blazor.radzen.com/api/toc.md)

## Examples

## Table of Contents

The Blazor ToC auto-generates a table of contents from the headings on the current page.

### Sticky TOC

To make the component stick to the top of a scrolling container, add `Style="position: sticky; top: 0;"`.

```razor
<RadzenLayout>
    <RadzenHeader>
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0">
            <RadzenSidebarToggle Click="@(() => sidebar1Expanded = !sidebar1Expanded)" />
            <RadzenLabel Text="Header" />
        </RadzenStack>
    </RadzenHeader>
    <RadzenSidebar @bind-Expanded="@sidebar1Expanded">
        <div class="rz-p-4">
            Sidebar
        </div>
    </RadzenSidebar>
    <RadzenBody class="inner-body" style="position: relative;">
        <RadzenRow class="rz-w-75 rz-w-sm-100">
            <RadzenColumn Size="8" SizeSM="6" SizeMD="8" SizeLG="9" Order="2" OrderSM="1">
                <RadzenText TextStyle="TextStyle.H2" TagName="TagName.H5">
                    Title
                </RadzenText>
                <RadzenText TextStyle="TextStyle.H4" TagName="TagName.H5" Anchor="toc#config-section1">
                    Section 1
                </RadzenText>
                <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P" class="rz-py-8">
                    Section 1 content
                </RadzenText>
                <RadzenText TextStyle="TextStyle.H4" TagName="TagName.H5" Anchor="toc#config-section2">
                    Section 2
                </RadzenText>
                <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P" class="rz-py-8">
                    Section 2 content
                </RadzenText>
                <RadzenText TextStyle="TextStyle.H4" TagName="TagName.H5" Anchor="toc#config-section3">
                    Section 3
                </RadzenText>
                <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P" class="rz-py-8">
                    Section 3 content
                </RadzenText>
                <RadzenText TextStyle="TextStyle.H4" TagName="TagName.H5" Anchor="toc#config-section4">
                    Section 4
                </RadzenText>
                <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P" class="rz-py-8">
                    Section 4 content
                </RadzenText>
                <RadzenText TextStyle="TextStyle.H4" TagName="TagName.H5" Anchor="toc#config-section5">
                    Section 5
                </RadzenText>
                <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P" class="rz-py-8">
                    Section 5 content
                </RadzenText>
                <h5 id="config-html-heading">HTML heading</h5>
                <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P" class="rz-py-8">
                    HTML heading content
                </RadzenText>
                <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P" class="rz-py-12">
                    Copyright &copy; 2026 Radzen Ltd. All rights reserved.
                </RadzenText>
            </RadzenColumn>
            <RadzenColumn Size="8" SizeSM="6" SizeMD="4" SizeLG="3" Order="1" OrderSM="2">
                <RadzenToc Selector=".inner-body" style="position: sticky; top: 0; right: 0;">
                    <RadzenTocItem Text="Section 1" Selector="#config-section1"></RadzenTocItem>
                    <RadzenTocItem Text="Section 2" Selector="#config-section2"></RadzenTocItem>
                    <RadzenTocItem Text="Section 3" Selector="#config-section3"></RadzenTocItem>
                    <RadzenTocItem Text="Section 4" Selector="#config-section4"></RadzenTocItem>
                    <RadzenTocItem Text="Section 5" Selector="#config-section5"></RadzenTocItem>
                    <RadzenTocItem Text="HTML Heading" Selector="#config-html-heading"></RadzenTocItem>
                </RadzenToc>
            </RadzenColumn>
        </RadzenRow>
    </RadzenBody>
</RadzenLayout>

@code {
    bool sidebar1Expanded = true;
}
```


### Orientation

RadzenToc supports a horizontal layout with built-in styling. Just set `Orientation="Orientation.Horizontal"` to enable it. In this demo the horizontal TOC is hidden on devices with screen width less than 1024px using utility CSS classes `class="rz-display-none rz-display-md-flex"`.

```razor
<RadzenLayout Style="grid-template-areas: 'rz-header rz-header' 'rz-body rz-body'">
    <RadzenHeader>
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0" class="rz-p-4">
            <RadzenLabel Text="Header" />
        </RadzenStack>
    </RadzenHeader>
    <RadzenBody class="horizontal-and-vertical-toc" Style="position: relative;">
        <RadzenToc Orientation="Orientation.Horizontal" Selector=".horizontal-and-vertical-toc" Style="position: sticky; top: 0; z-index: 1;" class="rz-display-none rz-display-md-flex">
            <RadzenTocItem Text="Section 1" Selector="#orientation-section1"></RadzenTocItem>
            <RadzenTocItem Text="Section 2" Selector="#orientation-section2"></RadzenTocItem>
            <RadzenTocItem Text="Section 3" Selector="#orientation-section3"></RadzenTocItem>
            <RadzenTocItem Text="Section 4" Selector="#orientation-section4"></RadzenTocItem>
            <RadzenTocItem Text="Section 5" Selector="#orientation-section5"></RadzenTocItem>
            <RadzenTocItem Text="HTML Heading" Selector="#orientation-html-heading"></RadzenTocItem>
        </RadzenToc>
        <RadzenRow class="rz-w-75 rz-w-sm-100">
            <RadzenColumn Size="8" SizeSM="6" SizeMD="8" SizeLG="9" Order="2" OrderSM="1">
                <RadzenText TextStyle="TextStyle.H2" TagName="TagName.H5">
                    Title
                </RadzenText>
                <RadzenText TextStyle="TextStyle.H4" TagName="TagName.H5" Anchor="toc#orientation-section1">
                    Section 1
                </RadzenText>
                <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P" class="rz-py-8">
                    Section 1 content
                </RadzenText>
                <RadzenText TextStyle="TextStyle.H4" TagName="TagName.H5" Anchor="toc#orientation-section2">
                    Section 2
                </RadzenText>
                <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P" class="rz-py-8">
                    Section 2 content
                </RadzenText>
                <RadzenText TextStyle="TextStyle.H4" TagName="TagName.H5" Anchor="toc#orientation-section3">
                    Section 3
                </RadzenText>
                <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P" class="rz-py-8">
                    Section 3 content
                </RadzenText>
                <RadzenText TextStyle="TextStyle.H4" TagName="TagName.H5" Anchor="toc#orientation-section4">
                    Section 4
                </RadzenText>
                <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P" class="rz-py-8">
                    Section 4 content
                </RadzenText>
                <RadzenText TextStyle="TextStyle.H4" TagName="TagName.H5" Anchor="toc#orientation-section5">
                    Section 5
                </RadzenText>
                <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P" class="rz-py-8">
                    Section 5 content
                </RadzenText>
                <h5 id="orientation-html-heading">HTML heading</h5>
                <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P" class="rz-py-8">
                    HTML heading content
                </RadzenText>
                <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P" class="rz-py-12">
                    Copyright &copy; 2026 Radzen Ltd. All rights reserved.
                </RadzenText>
            </RadzenColumn>
            <RadzenColumn Size="8" SizeSM="6" SizeMD="4" SizeLG="3" Order="1" OrderSM="2">
                <RadzenToc Orientation="Orientation.Vertical" Selector=".horizontal-and-vertical-toc" Style="position: sticky; top: 5rem; right: 0;">
                    <RadzenTocItem Text="Section 1" Selector="#orientation-section1"></RadzenTocItem>
                    <RadzenTocItem Text="Section 2" Selector="#orientation-section2"></RadzenTocItem>
                    <RadzenTocItem Text="Section 3" Selector="#orientation-section3"></RadzenTocItem>
                    <RadzenTocItem Text="Section 4" Selector="#orientation-section4"></RadzenTocItem>
                    <RadzenTocItem Text="Section 5" Selector="#orientation-section5"></RadzenTocItem>
                    <RadzenTocItem Text="HTML Heading" Selector="#orientation-html-heading"></RadzenTocItem>
                </RadzenToc>
            </RadzenColumn>
        </RadzenRow>
    </RadzenBody>
</RadzenLayout>
```
