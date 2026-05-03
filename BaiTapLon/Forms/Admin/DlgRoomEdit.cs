using BaiTapLon.Models;

namespace BaiTapLon.Forms.Admin;

public class DlgRoomEdit : Form
{
    private TextBox txtName = null!;
    private ComboBox cboType = null!;
    private NumericUpDown nudRows = null!;
    private NumericUpDown nudCols = null!;
    private NumericUpDown nudVipFrom = null!;
    private CheckBox chkCouple = null!;

    public Room RoomData { get; private set; } = new();
    public int VipFromRow => (int)nudVipFrom.Value;
    public bool CoupleLastRow => chkCouple.Checked;
    private readonly Room? _edit;

    public DlgRoomEdit(Room? edit) { _edit = edit; Init(); if (_edit != null) Load(); }

    private void Init()
    {
        this.Text = _edit == null ? "Thêm phòng chiếu" : "Sửa phòng";
        this.ClientSize = new Size(400, 340);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false; this.MinimizeBox = false;
        this.BackColor = Color.FromArgb(24, 24, 40);
        this.ForeColor = Color.FromArgb(200, 200, 220);

        int x1 = 20, x2 = 170, y = 20;
        Lbl("Tên phòng *", x1, y); txtName = Txt(x2, y, 200); y += 45;
        Lbl("Loại phòng", x1, y);
        cboType = new ComboBox { Font = new Font("Segoe UI", 11), Size = new Size(130, 30), Location = new Point(x2, y), BackColor = Color.FromArgb(35, 35, 55), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, DropDownStyle = ComboBoxStyle.DropDownList };
        cboType.Items.AddRange(new[] { "2D", "3D", "IMAX" }); cboType.SelectedIndex = 0;
        this.Controls.Add(cboType); y += 45;

        bool n = _edit == null;
        Lbl("Số hàng", x1, y); nudRows = Nud(x2, y, 3, 20, 8); nudRows.Enabled = n; y += 45;
        Lbl("Số cột", x1, y); nudCols = Nud(x2, y, 5, 20, 10); nudCols.Enabled = n; y += 45;
        Lbl("VIP từ hàng", x1, y); nudVipFrom = Nud(x2, y, 0, 20, 5); nudVipFrom.Enabled = n; y += 40;
        chkCouple = new CheckBox { Text = "Hàng cuối là Couple", Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(180, 180, 200), Location = new Point(x1, y), AutoSize = true, Checked = true, Enabled = n };
        this.Controls.Add(chkCouple);

        var btnOk = new Button { Text = n ? "Tạo" : "Lưu", Font = new Font("Segoe UI", 11, FontStyle.Bold), Size = new Size(100, 36), Location = new Point(170, 295), BackColor = Color.FromArgb(80, 160, 80), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.OK };
        btnOk.FlatAppearance.BorderSize = 0;
        btnOk.Click += (s, e) => { if (string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("Nhập tên!"); this.DialogResult = DialogResult.None; return; } RoomData = new Room { Name = txtName.Text.Trim(), Type = cboType.SelectedItem?.ToString() ?? "2D", Rows = (int)nudRows.Value, Columns = (int)nudCols.Value, IsActive = true }; };
        this.Controls.Add(btnOk);

        var btnC = new Button { Text = "Hủy", Font = new Font("Segoe UI", 10), Size = new Size(80, 36), Location = new Point(280, 295), BackColor = Color.FromArgb(50, 50, 75), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
        btnC.FlatAppearance.BorderSize = 0; this.Controls.Add(btnC);
        this.AcceptButton = btnOk; this.CancelButton = btnC;
    }

    private new void Load() { txtName.Text = _edit!.Name; var i = cboType.Items.IndexOf(_edit.Type); cboType.SelectedIndex = i >= 0 ? i : 0; nudRows.Value = _edit.Rows; nudCols.Value = _edit.Columns; }
    private void Lbl(string t, int x, int y) { this.Controls.Add(new Label { Text = t, Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(160, 160, 185), Location = new Point(x, y + 4), AutoSize = true }); }
    private TextBox Txt(int x, int y, int w) { var t = new TextBox { Font = new Font("Segoe UI", 11), Size = new Size(w, 30), Location = new Point(x, y), BackColor = Color.FromArgb(35, 35, 55), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle }; this.Controls.Add(t); return t; }
    private NumericUpDown Nud(int x, int y, int min, int max, int v) { var n = new NumericUpDown { Font = new Font("Segoe UI", 11), Size = new Size(80, 30), Location = new Point(x, y), BackColor = Color.FromArgb(35, 35, 55), ForeColor = Color.White, Minimum = min, Maximum = max, Value = v }; this.Controls.Add(n); return n; }
}
