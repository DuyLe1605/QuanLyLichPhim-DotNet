using QRCoder;

namespace BaiTapLon.Helpers;

/// <summary>
/// Helper sinh mã QR dạng hình ảnh (Bitmap) từ chuỗi bất kỳ.
/// Dùng cho: thẻ thành viên, mã đặt vé, voucher.
/// </summary>
public static class BarcodeHelper
{
    /// <summary>
    /// Sinh QR Code dạng Bitmap từ chuỗi (VD: "CM-AB12CD", "BK-1A2B3C").
    /// </summary>
    /// <param name="content">Nội dung mã hóa vào QR.</param>
    /// <param name="pixelsPerModule">Kích thước mỗi ô vuông (pixel). Mặc định 10.</param>
    /// <returns>Bitmap chứa QR Code, hoặc null nếu lỗi.</returns>
    public static Bitmap? GenerateQrCode(string content, int pixelsPerModule = 10)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;

        try
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
            using var qrCode = new PngByteQRCode(data);
            var pngBytes = qrCode.GetGraphic(pixelsPerModule);

            using var ms = new MemoryStream(pngBytes);
            return new Bitmap(ms);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Sinh QR Code với kích thước cố định (scale to fit).
    /// </summary>
    public static Bitmap? GenerateQrCode(string content, int width, int height)
    {
        var qr = GenerateQrCode(content, 20);
        if (qr == null) return null;

        var resized = new Bitmap(width, height);
        using var g = Graphics.FromImage(resized);
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
        g.DrawImage(qr, 0, 0, width, height);
        qr.Dispose();

        return resized;
    }

    /// <summary>
    /// Sinh mã booking code ngẫu nhiên dạng BK-XXXXXX.
    /// </summary>
    public static string GenerateBookingCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return "BK-" + new string(Enumerable.Range(0, 6)
            .Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }
}
