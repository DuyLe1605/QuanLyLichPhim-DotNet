using BaiTapLon.Helpers;
using BaiTapLon.Services;

namespace BaiTapLon.Forms;

public class DlgCustomerRegister : Form
{
    private readonly TextBox txtFullName = null!;
    private readonly TextBox txtUsername = null!;
    private readonly TextBox txtEmail = null!;
    private readonly TextBox txtPhone = null!;
    private readonly TextBox txtPassword = null!;
    private readonly TextBox txtConfirm = null!;
    private readonly Label lblError = null!;
    private readonly Button btnRegister = null!;
    private readonly ErrorProvider errorProvider = new();

    public DlgCustomerRegister()
    {
        Text = "Đăng ký khách hàng";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(460, 590);
        MinimumSize = new Size(460, 590);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(18, 18, 30);
        Font = new Font("Segoe UI", 10);

        var title = new Label
        {
            Text = "Tạo tài khoản CineMember",
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.FromArgb(235, 235, 245),
            Location = new Point(34, 25),
            Size = new Size(390, 36)
        };
        Controls.Add(title);

        var subtitle = new Label
        {
            Text = "Dùng tên đăng nhập để đăng nhập, đặt vé và xem lịch sử vé.",
            ForeColor = Color.FromArgb(145, 145, 170),
            Location = new Point(36, 65),
            Size = new Size(390, 24)
        };
        Controls.Add(subtitle);

        txtFullName = AddField("Họ tên", 112, false);
        txtUsername = AddField("Tên đăng nhập", 180, false);
        txtEmail = AddField("Email", 248, false);
        txtPhone = AddField("Số điện thoại", 316, false);
        txtPassword = AddField("Mật khẩu", 384, true);
        txtConfirm = AddField("Xác nhận mật khẩu", 452, true);

        lblError = new Label
        {
            ForeColor = Color.FromArgb(255, 95, 95),
            Location = new Point(36, 528),
            Size = new Size(390, 24),
            TextAlign = ContentAlignment.MiddleCenter,
            Visible = false
        };
        Controls.Add(lblError);

        btnRegister = new Button
        {
            Text = "ĐĂNG KÝ",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            BackColor = Color.FromArgb(100, 80, 255),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Location = new Point(36, 556),
            Size = new Size(250, 42),
            Cursor = Cursors.Hand
        };
        btnRegister.FlatAppearance.BorderSize = 0;
        btnRegister.Click += async (s, e) => await RegisterAsync();
        Controls.Add(btnRegister);

        var btnCancel = new Button
        {
            Text = "Hủy",
            Font = new Font("Segoe UI", 10),
            BackColor = Color.FromArgb(45, 45, 65),
            ForeColor = Color.FromArgb(220, 220, 235),
            FlatStyle = FlatStyle.Flat,
            Location = new Point(296, 556),
            Size = new Size(128, 42),
            DialogResult = DialogResult.Cancel,
            Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        Controls.Add(btnCancel);

        AcceptButton = btnRegister;
        CancelButton = btnCancel;
    }

    private TextBox AddField(string label, int y, bool password)
    {
        Controls.Add(new Label
        {
            Text = label,
            ForeColor = Color.FromArgb(160, 160, 185),
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Location = new Point(36, y),
            Size = new Size(390, 22)
        });

        var input = new TextBox
        {
            BackColor = Color.FromArgb(35, 35, 55),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 12),
            Location = new Point(36, y + 25),
            Size = new Size(388, 32),
            UseSystemPasswordChar = password
        };
        Controls.Add(input);
        return input;
    }

    private async Task RegisterAsync()
    {
        if (!ValidateInput()) return;

        btnRegister.Enabled = false;
        btnRegister.Text = "ĐANG TẠO...";
        lblError.Visible = false;

        try
        {
            using var context = Program.CreateDbContext();
            var service = new CustomerService(context);
            var result = await service.RegisterAsync(
                txtFullName.Text.Trim(),
                txtEmail.Text.Trim(),
                txtPhone.Text.Trim(),
                txtPassword.Text,
                txtUsername.Text.Trim());

            if (!result.Success)
            {
                ShowError(result.Message);
                return;
            }

            SessionManager.LoginAsCustomer(result.Customer!);
            MessageBox.Show(result.Message, "Đăng ký thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            btnRegister.Enabled = true;
            btnRegister.Text = "ĐĂNG KÝ";
        }
    }

    private bool ValidateInput()
    {
        errorProvider.Clear();
        lblError.Visible = false;

        var ok = true;
        if (string.IsNullOrWhiteSpace(txtFullName.Text))
        {
            errorProvider.SetError(txtFullName, "Nhập họ tên.");
            ok = false;
        }

        if (!CustomerService.IsValidUsername(txtUsername.Text.Trim()))
        {
            errorProvider.SetError(txtUsername, "Tên đăng nhập không hợp lệ.");
            ok = false;
        }

        if (string.IsNullOrWhiteSpace(txtEmail.Text) || !txtEmail.Text.Contains('@'))
        {
            errorProvider.SetError(txtEmail, "Email không hợp lệ.");
            ok = false;
        }

        if (string.IsNullOrWhiteSpace(txtPhone.Text) || txtPhone.Text.Trim().Length < 9)
        {
            errorProvider.SetError(txtPhone, "Số điện thoại không hợp lệ.");
            ok = false;
        }

        if (txtPassword.Text.Length < 6)
        {
            errorProvider.SetError(txtPassword, "Mật khẩu tối thiểu 6 ký tự.");
            ok = false;
        }

        if (txtPassword.Text != txtConfirm.Text)
        {
            errorProvider.SetError(txtConfirm, "Mật khẩu xác nhận không khớp.");
            ok = false;
        }

        if (!ok) ShowError("Vui lòng kiểm tra lại thông tin đăng ký.");
        return ok;
    }

    private void ShowError(string message)
    {
        lblError.Text = message;
        lblError.Visible = true;
    }
}
