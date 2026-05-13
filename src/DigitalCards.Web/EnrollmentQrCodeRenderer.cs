using QRCoder;

namespace DigitalCards.Web;

public static class EnrollmentQrCodeRenderer
{
    public static string RenderSvg(string value)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(value, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new SvgQRCode(data);
        return qrCode.GetGraphic(6, "#111827", "#ffffff", drawQuietZones: true);
    }
}
