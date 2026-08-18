# QRCode

Generate and display QR codes as SVG using RadzenQRCode.

Keywords: qr, qrcode, barcode, svg

> API reference: [RadzenQRCode API](https://blazor.radzen.com/api/qrcode.md)

## Examples

## Blazor QRCode

Generate and display QR codes as SVG images.

### Basic

Use the `Value` property to specify the data to encode in the QR code.

```razor
<RadzenStack Orientation="Orientation.Horizontal">
    <RadzenStack Orientation="Orientation.Vertical">
        <RadzenFieldset Text="@url">
            <RadzenQRCode Value="@url" />
        </RadzenFieldset>
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Vertical">
        <RadzenFieldset Text="@email">
            <RadzenQRCode Value="@email" />
        </RadzenFieldset>
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Vertical">
        <RadzenFieldset Text="@tel">
            <RadzenQRCode Value="@tel" />
        </RadzenFieldset>
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Vertical">
        <RadzenFieldset Text="@geo">
            <RadzenQRCode Value="@geo" />
        </RadzenFieldset>
    </RadzenStack>
</RadzenStack>

@code{
    string url = "https://blazor.radzen.com";
    string email = "mailto:info@radzen.com";
    string tel = "tel:+1-111-222-3333";
    string geo = "geo:51.5074,-0.1278,100";
}
```


### Long strings

Use the `Ecc="RadzenQREcc.Low"` for long strings.

```razor
<RadzenQRCode Value="@longStr" Ecc="RadzenQREcc.Low" Style="height:500px"/>

@code{
    string longStr = "http://www.reallylong.link/rll/10MKJ7KK3YzjXavgHow_nwpq3zTuG75n1Va/mMC_JbFn/1NZNXc8J0nu/3/5bDwyTHX00gQ_xoYvadGCXTXkmYXBCLuzRv9LSImZe8xf7dLsolDrjCw8y9J_P2YVREx4zcz3FpkSkqSjb_cUNMb47Ih1AxIwcb69H33/crc3B409Q84kgIEeYCQ6oOjzGCdZv4U7ilkBELPGk07yT1CMTkF2JADNc7kjNNV2NsbwviCU840cEQlOx6abAOU9gzNrgaivJZEhBQCZfEvmhUCfICi6GxaVYeLpVgxfw6enQXsuydAzAKrUbfyFZbPf8wYnz6Ap0mKZpR1uLY25Bu35nbKirbQYJgd_rSxXPlsvqJrUiKWrFxHzton6HQE17kFGB0kxpq/136N3SsGTVBO1j_gzJdNYk6VfzfCvn1_wB7u0amxwpwrWWzKnI3YViJDZTIz6BFnAQU04146EjD2hUGQ7tvrL7odN4MZ8kksu7WTWiw3FY_ZN2MLKoo3Tzpw9iSZFfMOhKdE3N3pIvRELxTr8GtIU3Ie0uL3I3wuDCPpr2uoW0UqINBAVWO93r6Neouw6sry6uEsTaQEvzOlunsi0O7ERKDWrZB4cMd6SlJuq72B2VNzpOdZhQGY_iA7daW74wHyr3MJncrz_McfsNlJEb9qeYDeqXu6JhOTkWWsc8f7FpzuWhwf9FNNDgi3FYVc2JN9ZBEzCt5BU2Om55EK9Ir9607gHKI2PvaOJ1QbFvbzCODyUYJ/wL9PkNsWLhR2yxiw0OuuPAQDdQgwZbbOijdv4iH6_p2ojtjOIHVMucGWQIJtna/zU63HbE2hHdPMXjSPrkZhZp4UGb68ISTDVz5dQDJvCMbUufSBb2u9tD_5HiQPyMJJjwrdzWFRStIVl2/wch4plIcmzKnTDAiCpgFaSrQmckbesueJq6O8zbIhISMgcPBgEL0D7I2sIJbIU1Fuetf3Y5hV9ijGoYC38LTux1N8PdguelAtcg4n9Sl9K9WX/vG4h9mDRACE4R6DLywyiZbAp6J3yz2AH0ZHBnTKaJ8wcWGqGCRb6k0YzZR3aYgTxxluYrDP6g8mMbGQHPwylOh8J5JlSCEkmypqUam1QUgElZ_0EwYtKvuQJ4vjljxTlPH9US6nCPoJQ/Ildb4fzWWnpHZQDxskmqJJe6fWXagmM582JOqm7ZMO2ND25vjn_PtaLjp6ngPy8x6ahh_h9avfJ2oyoBdpAqHyvyQSIJnAIealDQ/S2D2upBurWA7WwxRns9CyKkIzw9t2GnDlTKMpAWNCY6j34nTwDLOaopMh_bNVscYcgjEU/LuLiTBOnxMDYgf_T8oy73Juh5w0_VoVurVGKm9gLvrH_7TJaqnmWN4ACDvBFkVwG7eiCgCZKibT2jEivA8eBD_NGVMs3IeOUHogaom0dqMpGZVY7jEAnAn1UYmhf7LdTWdVSUXLUXwbNIC/zpzc9HAIzv0nUoGRwLOFGhsDD9fRKnihLPIOH6VvHoGquYkNnvBO2RXF5h_fc7PGnMojWWq/JglLfoSI2kudz98bCmqiKKcRh7n4MsFrH01W_e5yg_6ax5AFouJU_hc104KAzmQbKbdyOhhI5n9vKzDLn4OEngJO3ezCcCLBy8swtlI/c9Kr7W1KaXO_YjOCAJwIWjjnEKwOgN/GmjYkpUD8ux6pQfMh3nsLTZY7UwAaGtbaQab9CDTzuudqjcfsEoM5cQGx5f1U_LCnzvlh29nloLKBdaB72ZSj5P9KOZcVHQ22CZlm6uX2dEWBjwi_NAH_AzW47BsBDsxFgpwYcoheqnhQ_xsY4c894R1TE37ASm6VhXKdkNGR7O6lE/HA2KUf2LSSLWHiSrOB5XpRRcT0s1d3HVkLnTvMwhjXJS1EWZm8UKoVQ6bKJwfE6G9NL/LL0JpXUQ7M9GKJ9AxIetjFLOMt5MSXytI2tLLzqWBI4CHYPj2v";
}
```


### Styled

Customize the QR code appearance with `Foreground`, `Background`, `ModuleShape`, `EyeShape`, `EyeColor` and `Size` properties.

```razor
<RadzenRow Gap="2rem" AlignItems="AlignItems.Start">
    <RadzenColumn Size="12" SizeMD="6">
        <RadzenStack AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Style="height: 100%;">
            <RadzenQRCode @ref="qrCode" Value="Radzen Blazor"
                          Foreground="@foreground"
                          Background="@background"
                          EyeColor="@eyeColor"
                          ModuleShape="@moduleShape"
                          EyeShape="@eyeShape"
                          EyeShapeTopLeft="@topLeftEyeShape"
                          EyeShapeTopRight="@topRightEyeShape"
                          EyeShapeBottomLeft="@bottomLeftEyeShape"
                          EyeColorTopLeft="@topLeftEyeColor"
                          EyeColorTopRight="@topRightEyeColor"
                          EyeColorBottomLeft="@bottomLeftEyeColor"
                          Image="@imageUrl"
                          ImageBackground="@imageBackground" />
        </RadzenStack>
    </RadzenColumn>
    <RadzenColumn Size="12" SizeMD="6">
        <RadzenStack Gap="1rem">
            <RadzenFieldset Text="Colors">
                <RadzenStack Gap="1rem">
                    <RadzenRow>
                        <RadzenColumn Size="12" SizeMD="6">
                            <RadzenFormField Text="Foreground" Variant="@Variant.Outlined" class="rz-w-100">
                                <RadzenColorPicker @bind-Value="@foreground" ShowHSV="true" ShowRGBA="true" ShowColors="true" />
                            </RadzenFormField>
                        </RadzenColumn>
                        <RadzenColumn Size="12" SizeMD="6">
                            <RadzenFormField Text="Background" Variant="@Variant.Outlined" class="rz-w-100">
                                <RadzenColorPicker @bind-Value="@background" ShowHSV="true" ShowRGBA="true" ShowColors="true" />
                            </RadzenFormField>
                        </RadzenColumn>
                        <RadzenColumn Size="12" SizeMD="6">
                            <RadzenFormField Text="Eye Color" Variant="@Variant.Outlined" class="rz-w-100">
                                <RadzenColorPicker @bind-Value="@eyeColor" ShowHSV="true" ShowRGBA="true" ShowColors="true" />
                            </RadzenFormField>
                        </RadzenColumn>
                        <RadzenColumn Size="12" SizeMD="6">
                            <RadzenFormField Text="Top Left Eye Color" Variant="@Variant.Outlined" class="rz-w-100">
                                <RadzenColorPicker @bind-Value="@topLeftEyeColor" ShowHSV="true" ShowRGBA="true" ShowColors="true" />
                            </RadzenFormField>
                        </RadzenColumn>
                        <RadzenColumn Size="12" SizeMD="6">
                            <RadzenFormField Text="Top Right Eye Color" Variant="@Variant.Outlined" class="rz-w-100">
                                <RadzenColorPicker @bind-Value="@topRightEyeColor" ShowHSV="true" ShowRGBA="true" ShowColors="true" />
                            </RadzenFormField>
                        </RadzenColumn>
                        <RadzenColumn Size="12" SizeMD="6">
                            <RadzenFormField Text="Bottom Left Eye Color" Variant="@Variant.Outlined" class="rz-w-100">
                                <RadzenColorPicker @bind-Value="@bottomLeftEyeColor" ShowHSV="true" ShowRGBA="true" ShowColors="true" />
                            </RadzenFormField>
                        </RadzenColumn>
                    </RadzenRow>
                    <RadzenButton Text="Reset Colors" Variant="Variant.Flat" ButtonStyle="ButtonStyle.Base" Icon="refresh" Click="@ResetColors" />
                </RadzenStack>
            </RadzenFieldset>

            <RadzenFieldset Text="Shapes">
                <RadzenStack Gap="1rem">
                    <RadzenLabel Text="Module Shape" />
                    <RadzenSelectBar @bind-Value="@moduleShape" Data="@moduleShapes" />
                    
                    <RadzenLabel Text="Eye Shape" />
                    <RadzenSelectBar @bind-Value="@eyeShape" Data="@eyeShapes" />
                    
                    <RadzenLabel Text="Top Left Eye Shape" />
                    <RadzenSelectBar @bind-Value="@topLeftEyeShape">
                        <Items>
                            <RadzenSelectBarItem Text="Inherit" Value="@((QRCodeEyeShape?)null)" />
                            <RadzenSelectBarItem Text="Framed" Value="@QRCodeEyeShape.Framed" />
                            <RadzenSelectBarItem Text="Rounded" Value="@QRCodeEyeShape.Rounded" />
                            <RadzenSelectBarItem Text="Square" Value="@QRCodeEyeShape.Square" />
                        </Items>
                    </RadzenSelectBar>
                    
                    <RadzenLabel Text="Top Right Eye Shape" />
                    <RadzenSelectBar @bind-Value="@topRightEyeShape">
                        <Items>
                            <RadzenSelectBarItem Text="Inherit" Value="@((QRCodeEyeShape?)null)" />
                            <RadzenSelectBarItem Text="Framed" Value="@QRCodeEyeShape.Framed" />
                            <RadzenSelectBarItem Text="Rounded" Value="@QRCodeEyeShape.Rounded" />
                            <RadzenSelectBarItem Text="Square" Value="@QRCodeEyeShape.Square" />
                        </Items>
                    </RadzenSelectBar>
                    
                    <RadzenLabel Text="Bottom Left Eye Shape" />
                    <RadzenSelectBar @bind-Value="@bottomLeftEyeShape">
                        <Items>
                            <RadzenSelectBarItem Text="Inherit" Value="@((QRCodeEyeShape?)null)" />
                            <RadzenSelectBarItem Text="Framed" Value="@QRCodeEyeShape.Framed" />
                            <RadzenSelectBarItem Text="Rounded" Value="@QRCodeEyeShape.Rounded" />
                            <RadzenSelectBarItem Text="Square" Value="@QRCodeEyeShape.Square" />
                        </Items>
                    </RadzenSelectBar>
                </RadzenStack>
            </RadzenFieldset>

            <RadzenFieldset Text="Center Image">
                <RadzenStack Gap="1rem">
                    <RadzenFormField Text="Image" Variant="@Variant.Outlined">
                        <RadzenUpload Change="@OnImageChange" Accept="image/*" />
                    </RadzenFormField>
                    <RadzenFormField Text="Image Background" Variant="@Variant.Outlined">
                        <RadzenColorPicker @bind-Value="@imageBackground" ShowHSV="true" ShowRGBA="true" ShowColors="true" />
                    </RadzenFormField>
                </RadzenStack>
            </RadzenFieldset>

            <RadzenButton Text="Save SVG"
                          Icon="download"
                          ButtonStyle="ButtonStyle.Primary"
                          Click="@(_ => SaveSvg())" />
        </RadzenStack>
    </RadzenColumn>
</RadzenRow>

@code {
    RadzenQRCode qrCode;

    string foreground = "#0f62fe";
    string background = "#eef4ff";

    string eyeColor = "#0f62fe";
    string topLeftEyeColor;
    string topRightEyeColor;
    string bottomLeftEyeColor;

    QRCodeModuleShape moduleShape = QRCodeModuleShape.Square;

    QRCodeEyeShape eyeShape = QRCodeEyeShape.Square;
    QRCodeEyeShape? topLeftEyeShape;
    QRCodeEyeShape? topRightEyeShape;
    QRCodeEyeShape? bottomLeftEyeShape;

    string imageUrl;
    string imageBackground = "#FFFFFF";

    QRCodeModuleShape[] moduleShapes = Enum.GetValues<QRCodeModuleShape>();
    QRCodeEyeShape[] eyeShapes = Enum.GetValues<QRCodeEyeShape>();
    QRCodeEyeShape?[] nullableEyeShapes = new QRCodeEyeShape?[] { null, QRCodeEyeShape.Framed, QRCodeEyeShape.Rounded, QRCodeEyeShape.Square};

    async Task OnImageChange(UploadChangeEventArgs args)
    {
        var file = args.Files.FirstOrDefault();
        if (file != null)
        {
            var buffers = new byte[file.Size];
            await file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024).ReadAsync(buffers);
            var base64 = Convert.ToBase64String(buffers);
            imageUrl = $"data:{file.ContentType};base64,{base64}";
        }
    }

    void ResetColors()
    {
        foreground = "#0f62fe";
        background = "#eef4ff";
        eyeColor = "#0f62fe";
        topLeftEyeColor = null;
        topRightEyeColor = null;
        bottomLeftEyeColor = null;
        imageBackground = "#FFFFFF";
    }

    async Task SaveSvg(bool custom = false)
    {
        const string value = "Radzen Blazor";
        var modules = RadzenQREncoder.EncodeUtf8(value, RadzenQREcc.Quartile);
        var svg = custom ? RadzenQREncoder.ToSvg(
            modules,
            moduleSize: 8,
            foreground: foreground,
            background: background,
            moduleShape: moduleShape,
            eyeShape: eyeShape,
            eyeShapeTopLeft: topLeftEyeShape,
            eyeShapeTopRight: topRightEyeShape,
            eyeShapeBottomLeft: bottomLeftEyeShape,
            eyeColor: eyeColor,
            eyeColorTopLeft: topLeftEyeColor,
            eyeColorTopRight: topRightEyeColor,
            eyeColorBottomLeft: bottomLeftEyeColor,
            image: imageUrl,
            imageBackground: imageBackground) : await qrCode.ToSvg();

        await JS.InvokeVoidAsync("Radzen.downloadFile", "qrcode.svg", svg, "image/svg+xml;charset=utf-8");
    }
}
```
