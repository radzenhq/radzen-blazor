# SSRS Viewer

Demonstration and configuration of Radzen SSRS Viewer Radzen Blazor Arc Gauge component.

Keywords: report, ssrs

> API reference: [RadzenSSRSViewer API](https://blazor.radzen.com/api/ssrsviewer.md)

## Examples

## SSRS Viewer

RadzenSSRSViewer displays a report created in SQL Server Reporting Services (SSRS).
To display a report you should specify:

### Parameters

Often SSRS reports have parameters. You can specify those via the `Parameters` collection:

### Proxy

Often your report server won't be exposed to the public Internet or you would want to either hide report parameters or provide security credentials. In this case you can use the built-in proxy support in RadzenSSRSViewer. To enable it set the `UseProxy` property to `true` and add the `ReportController` class below to your Blazor application.

### Provide credentials

To provide user credentials when making the proxy requests you can implement the OnHttpClientHandlerCreate partial method of ReportController.
Alternatively you can set the `Credentials` property of `httpClientHandler` directly in the `CreateHttpClient` method of the `ReportController` class:
In some setups authenticating the request like this could fail with exceptions such as:
