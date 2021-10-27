# SSRSViewer component
This article demonstrates how to use SSRSViewer. Use this component to access Microsoft SQL Server Reporting Services.

```
<RadzenSSRSViewer ReportName="Untitled" ReportServer="http://localhost/ReportServer/" UseProxy="true">
  <Parameters>
    <RadzenSSRSViewerParameter ParameterName="Param1" Value="1" />
    <RadzenSSRSViewerParameter ParameterName="Param2" Value="2" />
  </Parameters>
</RadzenSSRSViewer>
```

When `UseProxy` is set to `true` a special `ReportController` will be accessed instead directly accessing report server URL.
