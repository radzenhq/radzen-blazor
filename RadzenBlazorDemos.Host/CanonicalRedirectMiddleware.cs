using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace RadzenBlazorDemos.Host;

/// <summary>
/// Redirects legacy and non-canonical demo routes to canonical routes.
/// </summary>
public static class CanonicalRedirectMiddleware
{
    private static readonly FrozenDictionary<string, string> StaticRedirects = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["/buttons"] = "/button",
        ["/docs"] = "/",
        ["/docs/guides/components/aichat.html"] = "/aichat",
        ["/docs/guides/components/accordion.html"] = "/accordion",
        ["/docs/guides/components/arcgauge.html"] = "/arc-gauge",
        ["/docs/guides/components/autocomplete.html"] = "/autocomplete",
        ["/docs/guides/components/badge.html"] = "/badge",
        ["/docs/guides/components/barcode.html"] = "/barcode",
        ["/docs/guides/components/breadcrumb.html"] = "/breadcrumb",
        ["/docs/guides/components/button.html"] = "/button",
        ["/docs/guides/components/carousel.html"] = "/carousel",
        ["/docs/guides/components/chart.html"] = "/chart-series",
        ["/docs/guides/components/chat.html"] = "/chat",
        ["/docs/guides/components/checkbox.html"] = "/checkbox",
        ["/docs/guides/components/checkboxlist.html"] = "/checkboxlist",
        ["/docs/guides/components/colorpicker.html"] = "/colorpicker",
        ["/docs/guides/components/comparevalidator.html"] = "/comparevalidator",
        ["/docs/guides/components/contextmenu.html"] = "/contextmenu",
        ["/docs/guides/components/datalist.html"] = "/datalist",
        ["/docs/guides/components/datagrid.html"] = "/datagrid",
        ["/docs/guides/components/datepicker.html"] = "/datepicker",
        ["/docs/guides/components/dialog.html"] = "/dialog",
        ["/docs/guides/components/dropdown.html"] = "/dropdown",
        ["/docs/guides/components/dropdowndatagrid.html"] = "/dropdown-datagrid",
        ["/docs/guides/components/emailvalidator.html"] = "/emailvalidator",
        ["/docs/guides/components/fieldset.html"] = "/fieldset",
        ["/docs/guides/components/fileinput.html"] = "/fileinput",
        ["/docs/guides/getting-started/context-menu.html"] = "/get-started",
        ["/docs/guides/getting-started/dialog.html"] = "/get-started",
        ["/docs/guides/getting-started/notification.html"] = "/get-started",
        ["/docs/guides/getting-started/tooltip.html"] = "/get-started",
        ["/docs/guides/getting-started/use-component.html"] = "/get-started",
        ["/docs/guides/components/googlemap.html"] = "/googlemap",
        ["/docs/guides/components/gravatar.html"] = "/gravatar",
        ["/docs/guides/components/htmleditor.html"] = "/html-editor",
        ["/docs/guides/components/icon.html"] = "/icon",
        ["/docs/guides/components/image.html"] = "/image",
        ["/docs/guides/index.html"] = "/get-started",
        ["/docs/guides/components/label.html"] = "/label",
        ["/docs/guides/components/lengthvalidator.html"] = "/lengthvalidator",
        ["/docs/guides/components/link.html"] = "/link",
        ["/docs/guides/components/listbox.html"] = "/listbox",
        ["/docs/guides/components/login.html"] = "/login",
        ["/docs/guides/components/mask.html"] = "/mask",
        ["/docs/guides/components/media-query.html"] = "/media-query",
        ["/docs/guides/components/menu.html"] = "/menu",
        ["/docs/guides/components/notification.html"] = "/notification",
        ["/docs/guides/components/numeric.html"] = "/numeric",
        ["/docs/guides/components/numericrangevalidator.html"] = "/numericrangevalidator",
        ["/docs/guides/components/panel.html"] = "/panel",
        ["/docs/guides/components/panelmenu.html"] = "/panelmenu",
        ["/docs/guides/components/pager.html"] = "/pager",
        ["/docs/guides/components/password.html"] = "/password",
        ["/docs/guides/components/profilemenu.html"] = "/profile-menu",
        ["/docs/guides/components/progressbar.html"] = "/progressbar",
        ["/docs/guides/components/qrcode.html"] = "/qrcode",
        ["/docs/guides/components/radiobuttonlist.html"] = "/radiobuttonlist",
        ["/docs/guides/components/radialgauge.html"] = "/radial-gauge",
        ["/docs/guides/components/rating.html"] = "/rating",
        ["/docs/guides/components/regexvalidator.html"] = "/regexvalidator",
        ["/docs/guides/components/requiredvalidator.html"] = "/requiredvalidator",
        ["/docs/guides/components/scheduler.html"] = "/scheduler",
        ["/docs/guides/components/selectbar.html"] = "/selectbar",
        ["/docs/guides/components/slider.html"] = "/slider",
        ["/docs/guides/components/splitbutton.html"] = "/splitbutton",
        ["/docs/guides/components/splitter.html"] = "/splitter",
        ["/docs/guides/components/ssrsviewer.html"] = "/ssrsviewer",
        ["/docs/guides/components/steps.html"] = "/steps",
        ["/docs/guides/components/switch.html"] = "/switch",
        ["/docs/guides/components/tabs.html"] = "/tabs",
        ["/docs/guides/components/templateform.html"] = "/templateform",
        ["/docs/guides/components/textarea.html"] = "/textarea",
        ["/docs/guides/components/textbox.html"] = "/textbox",
        ["/docs/guides/components/timespanpicker.html"] = "/timespanpicker",
        ["/docs/guides/components/tooltip.html"] = "/tooltip",
        ["/docs/guides/components/tree.html"] = "/tree",
        ["/docs/guides/components/upload.html"] = "/example-upload"
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public static IApplicationBuilder UseCanonicalRedirects(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            var path = NormalizePath(context.Request.Path.Value ?? string.Empty);

            if (TryResolveTarget(path, out var targetPath))
            {
                context.Response.StatusCode = StatusCodes.Status301MovedPermanently;
                context.Response.Headers.Location = targetPath + context.Request.QueryString;
                context.Response.Headers.CacheControl = "public, max-age=86400";
                return;
            }

            await next();
        });

        return app;
    }

    private static bool TryResolveTarget(string path, out string targetPath)
    {
        if (StaticRedirects.TryGetValue(path, out targetPath))
        {
            return true;
        }

        if (TryMapOptionalSegment(path, "/getting-started", "/get-started", out targetPath))
        {
            return true;
        }

        if (TryMapOptionalSegment(path, "/docs/guides/getting-started/installation.html", "/get-started", out targetPath))
        {
            return true;
        }

        if (TryStripApiHtmlExtension(path, out targetPath))
        {
            return true;
        }

        targetPath = string.Empty;
        return false;
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "/";
        }

        return path.TrimEnd('/') switch
        {
            "" => "/",
            var normalized => normalized
        };
    }

    private static bool TryStripApiHtmlExtension(string path, out string targetPath)
    {
        if (path.Equals("/docs/api/index.html", StringComparison.OrdinalIgnoreCase))
        {
            targetPath = "/docs/api";
            return true;
        }

        if (path.StartsWith("/docs/api/", StringComparison.OrdinalIgnoreCase) && path.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            targetPath = path[..^5];
            return true;
        }

        targetPath = string.Empty;
        return false;
    }

    private static bool TryMapOptionalSegment(string path, string sourceBasePath, string targetBasePath, out string targetPath)
    {
        if (path.Equals(sourceBasePath, StringComparison.OrdinalIgnoreCase))
        {
            targetPath = targetBasePath;
            return true;
        }

        var prefix = sourceBasePath + "/";
        if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            var suffix = path[prefix.Length..];
            if (!suffix.Contains('/'))
            {
                targetPath = targetBasePath + "/" + suffix;
                return true;
            }
        }

        targetPath = string.Empty;
        return false;
    }
}
