using ClosedXML.Excel;

namespace BaiTapLon.Helpers;

public static class ExcelHelper
{
    public static void ExportDataGridViewToExcel(DataGridView grid, string sheetName, string title)
    {
        using var sfd = new SaveFileDialog
        {
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            FileName = $"{sheetName}_{DateTime.Now:yyyyMMddHHmmss}.xlsx"
        };

        if (sfd.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add(sheetName);

            // Tựa đề
            ws.Cell(1, 1).Value = title;
            var titleRange = ws.Range(1, 1, 1, grid.Columns.GetColumnCount(DataGridViewElementStates.Visible));
            titleRange.Merge();
            titleRange.Style.Font.Bold = true;
            titleRange.Style.Font.FontSize = 16;
            titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Header
            int colIndex = 1;
            var visibleColumns = new List<DataGridViewColumn>();
            foreach (DataGridViewColumn col in grid.Columns)
            {
                if (col.Visible)
                {
                    ws.Cell(3, colIndex).Value = col.HeaderText;
                    ws.Cell(3, colIndex).Style.Font.Bold = true;
                    ws.Cell(3, colIndex).Style.Fill.BackgroundColor = XLColor.LightGray;
                    ws.Cell(3, colIndex).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    visibleColumns.Add(col);
                    colIndex++;
                }
            }

            // Data
            int rowIndex = 4;
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;

                colIndex = 1;
                foreach (var col in visibleColumns)
                {
                    var cellValue = row.Cells[col.Index].Value;
                    ws.Cell(rowIndex, colIndex).Value = cellValue?.ToString() ?? "";
                    ws.Cell(rowIndex, colIndex).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    colIndex++;
                }
                rowIndex++;
            }

            ws.Columns().AdjustToContents();
            workbook.SaveAs(sfd.FileName);

            MessageBox.Show("Xuất file Excel thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi xuất file Excel: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
