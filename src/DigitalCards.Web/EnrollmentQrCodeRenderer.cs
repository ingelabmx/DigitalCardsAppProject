using System.Globalization;
using System.Xml.Linq;
using QRCoder;

namespace DigitalCards.Web;

public static class EnrollmentQrCodeRenderer
{
    public static string RenderSvg(string value)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(value, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new SvgQRCode(data);
        return NormalizeSvg(qrCode.GetGraphic(6, "#111827", "#ffffff", drawQuietZones: true));
    }

    private static string NormalizeSvg(string svg)
    {
        var document = XDocument.Parse(svg);
        var root = document.Root ?? throw new InvalidOperationException("QR SVG could not be generated.");
        if (root.Attribute("viewBox") is null &&
            TryReadSvgDimension(root.Attribute("width")?.Value, out var width) &&
            TryReadSvgDimension(root.Attribute("height")?.Value, out var height))
        {
            root.SetAttributeValue("viewBox", FormattableString.Invariant($"0 0 {width} {height}"));
        }

        root.SetAttributeValue("width", "100%");
        root.SetAttributeValue("height", "100%");
        root.SetAttributeValue("preserveAspectRatio", "xMidYMid meet");

        return document.ToString(SaveOptions.DisableFormatting);
    }

    private static bool TryReadSvgDimension(string? value, out int dimension)
    {
        dimension = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var numeric = new string(value.TakeWhile(character =>
            char.IsDigit(character) ||
            character == '.').ToArray());

        return double.TryParse(numeric, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) &&
            parsed > 0 &&
            (dimension = (int)Math.Ceiling(parsed)) > 0;
    }
}
