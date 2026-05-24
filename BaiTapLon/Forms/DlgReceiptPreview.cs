using BaiTapLon.Helpers;

namespace BaiTapLon.Forms;

/// <summary>
/// Dialog hiển thị preview hóa đơn dạng thermal receipt.
/// Cho phép xuất PDF hoặc in hóa đơn.
/// </summary>
public class DlgReceiptPreview : Form
{
    private readonly ReceiptData _data;
    private PictureBox picPreview = null!;
    private Panel pnlActions = null!;

    public DlgReceiptPreview(ReceiptData data)
    {
        _data = data;
        InitializeComponent();
        _ = LoadPreviewAsync();
    }

    private void InitializeComponent()
    {
        Text = $"Hóa đơn — {_data.InvoiceCode}";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(440, 720);
        MinimumSize = new Size(380, 600);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        BackColor = Color.FromArgb(24, 24, 40);
        DoubleBuffered = true;
        ShowInTaskbar = false;

        // ── Actions panel (bottom) ────────────────────────────────────────
        pnlActions = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 64,
            BackColor = Color.FromArgb(18, 18, 32),
            Padding = new Padding(14, 10, 14, 10)
        };

        var flpButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent
        };

        var btnExportPdf = CreateActionButton("📄 Xuất PDF", Color.FromArgb(70, 145, 230));
        btnExportPdf.Click += BtnExportPdf_Click;
        flpButtons.Controls.Add(btnExportPdf);

        var btnPrint = CreateActionButton("🖨️ In hóa đơn", Color.FromArgb(100, 180, 120));
        btnPrint.Click += BtnPrint_Click;
        flpButtons.Controls.Add(btnPrint);

        var btnClose = CreateActionButton("Đóng", Color.FromArgb(80, 80, 100));
        btnClose.Click += (s, e) => Close();
        flpButtons.Controls.Add(btnClose);

        pnlActions.Controls.Add(flpButtons);
        Controls.Add(pnlActions);

        // ── Scroll panel with receipt preview ─────────────────────────────
        var scroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.FromArgb(38, 38, 55),
            Padding = new Padding(20, 16, 20, 16)
        };

        picPreview = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.White,
            Dock = DockStyle.Top,
            Height = 800, // will be adjusted after loading
            Cursor = Cursors.Hand
        };
        picPreview.Click += (s, e) => BtnExportPdf_Click(s, e);

        scroll.Controls.Add(picPreview);
        Controls.Add(scroll);

        // Loading label
        var lblLoading = new Label
        {
            Text = "Đang tạo hóa đơn...",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(150, 156, 176),
            Font = new Font("Segoe UI", 12),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.FromArgb(38, 38, 55),
            Name = "lblLoading"
        };
        scroll.Controls.Add(lblLoading);
        lblLoading.BringToFront();
    }

    private async Task LoadPreviewAsync()
    {
        try
        {
            // Run PDF generation on background thread
            var previewImage = await Task.Run(() => ReceiptBuilder.BuildPreviewBitmap(_data));

            if (IsDisposed) return;

            Invoke(() =>
            {
                picPreview.Image = previewImage;

                // Scale height to maintain aspect ratio within preview width
                var previewWidth = picPreview.Parent?.ClientSize.Width - 40 ?? 380;
                if (previewImage.Width > 0)
                {
                    var ratio = (double)previewImage.Height / previewImage.Width;
                    picPreview.Height = (int)(previewWidth * ratio);
                }

                // Remove loading label
                var scroll = picPreview.Parent;
                var loading = scroll?.Controls.Find("lblLoading", false).FirstOrDefault();
                if (loading != null)
                    scroll?.Controls.Remove(loading);
            });
        }
        catch (Exception ex)
        {
            if (IsDisposed) return;
            Invoke(() =>
            {
                var scroll = picPreview.Parent;
                var loading = scroll?.Controls.Find("lblLoading", false).FirstOrDefault();
                if (loading is Label lbl)
                    lbl.Text = $"Lỗi tạo hóa đơn: {ex.Message}";
            });
        }
    }

    private void BtnExportPdf_Click(object? sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Xuất hóa đơn PDF",
            Filter = "PDF Files|*.pdf",
            FileName = $"{_data.InvoiceCode}_{_data.CreatedAt:yyyyMMdd_HHmm}.pdf",
            DefaultExt = "pdf"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var pdfBytes = ReceiptBuilder.BuildPdf(_data);
            File.WriteAllBytes(dialog.FileName, pdfBytes);
            MessageBox.Show(
                $"Đã xuất hóa đơn thành công!\n\n{dialog.FileName}",
                "Xuất PDF",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Lỗi xuất PDF: {ex.Message}",
                "Lỗi",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void BtnPrint_Click(object? sender, EventArgs e)
    {
        try
        {
            var previewImage = picPreview.Image;
            if (previewImage == null)
            {
                MessageBox.Show("Chưa có hóa đơn để in.", "In hóa đơn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var printDoc = new System.Drawing.Printing.PrintDocument();
            printDoc.DefaultPageSettings.PaperSize = new System.Drawing.Printing.PaperSize("Receipt", 315, 0);
            printDoc.PrintPage += (s, pe) =>
            {
                if (pe.Graphics == null) return;

                var pageWidth = pe.PageBounds.Width - 20;
                var ratio = (float)previewImage.Height / previewImage.Width;
                var printHeight = (int)(pageWidth * ratio);

                pe.Graphics.DrawImage(previewImage, 10, 10, pageWidth, printHeight);
                pe.HasMorePages = false;
            };

            using var printDialog = new PrintDialog { Document = printDoc };
            if (printDialog.ShowDialog(this) == DialogResult.OK)
            {
                printDoc.Print();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Lỗi in hóa đơn: {ex.Message}",
                "Lỗi",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static Button CreateActionButton(string text, Color backColor)
    {
        var btn = new Button
        {
            Text = text,
            Width = 120,
            Height = 40,
            Margin = new Padding(0, 0, 10, 0),
            BackColor = backColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(
            Math.Min(backColor.R + 20, 255),
            Math.Min(backColor.G + 20, 255),
            Math.Min(backColor.B + 20, 255));
        return btn;
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        base.OnFormClosed(e);
        picPreview.Image?.Dispose();
    }
}
