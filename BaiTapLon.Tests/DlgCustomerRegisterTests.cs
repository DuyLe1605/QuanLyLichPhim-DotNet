using System.Reflection;
using BaiTapLon.Forms;

namespace BaiTapLon.Tests;

/// <summary>
/// Unit tests for DlgCustomerRegister validation logic.
/// Requirements: 1.3
/// </summary>
public class DlgCustomerRegisterTests
{
    // Helper: invoke the private ValidateInput() method via reflection.
    private static bool InvokeValidateInput(DlgCustomerRegister dialog)
    {
        var method = typeof(DlgCustomerRegister)
            .GetMethod("ValidateInput", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(method);
        return (bool)method.Invoke(dialog, null)!;
    }

    // Helper: set a private TextBox field's Text property via reflection.
    private static void SetFieldText(DlgCustomerRegister dialog, string fieldName, string text)
    {
        var field = typeof(DlgCustomerRegister)
            .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(field);
        var textBox = (TextBox)field.GetValue(dialog)!;
        textBox.Text = text;
    }

    // Fill all fields with valid values so that only the field under test is invalid.
    private static void FillValidFields(DlgCustomerRegister dialog)
    {
        SetFieldText(dialog, "txtFullName", "Nguyen Van A");
        SetFieldText(dialog, "txtUsername", "valid_user1");
        SetFieldText(dialog, "txtEmail", "test@example.com");
        SetFieldText(dialog, "txtPhone", "0912345678");
        SetFieldText(dialog, "txtPassword", "password123");
        SetFieldText(dialog, "txtConfirm", "password123");
    }

    // -------------------------------------------------------------------------
    // Requirement 1.3 — Registration dialog requires a username
    // -------------------------------------------------------------------------

    [Fact]
    public void ValidateInput_WithEmptyUsername_ReturnsFalse()
    {
        using var dialog = new DlgCustomerRegister();

        FillValidFields(dialog);
        // Override username with empty string
        SetFieldText(dialog, "txtUsername", "");

        var result = InvokeValidateInput(dialog);

        Assert.False(result);
    }

    [Fact]
    public void ValidateInput_WithWhitespaceOnlyUsername_ReturnsFalse()
    {
        using var dialog = new DlgCustomerRegister();

        FillValidFields(dialog);
        SetFieldText(dialog, "txtUsername", "   ");

        var result = InvokeValidateInput(dialog);

        Assert.False(result);
    }

    [Fact]
    public void ValidateInput_WithValidUsername_ReturnsTrue()
    {
        using var dialog = new DlgCustomerRegister();

        FillValidFields(dialog);
        // txtUsername is already set to "valid_user1" by FillValidFields

        var result = InvokeValidateInput(dialog);

        Assert.True(result);
    }

    [Fact]
    public void ValidateInput_WithEmptyUsername_SetsErrorOnUsernameField()
    {
        using var dialog = new DlgCustomerRegister();

        FillValidFields(dialog);
        SetFieldText(dialog, "txtUsername", "");

        InvokeValidateInput(dialog);

        // Verify the errorProvider has an error set on txtUsername
        var errorProviderField = typeof(DlgCustomerRegister)
            .GetField("errorProvider", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(errorProviderField);
        var errorProvider = (ErrorProvider)errorProviderField.GetValue(dialog)!;

        var txtUsernameField = typeof(DlgCustomerRegister)
            .GetField("txtUsername", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(txtUsernameField);
        var txtUsername = (TextBox)txtUsernameField.GetValue(dialog)!;

        var error = errorProvider.GetError(txtUsername);
        Assert.False(string.IsNullOrEmpty(error),
            "Expected an error message to be set on the username field.");
    }
}
