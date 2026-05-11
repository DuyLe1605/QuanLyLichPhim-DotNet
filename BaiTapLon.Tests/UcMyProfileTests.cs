using System.Reflection;
using BaiTapLon.Forms.Customer;
using BaiTapLon.Models;

namespace BaiTapLon.Tests;

/// <summary>
/// Unit tests for UcMyProfile username display.
/// Requirements: 1.7
/// </summary>
public class UcMyProfileTests
{
    // -------------------------------------------------------------------------
    // Requirement 1.7 — txtUsername is read-only
    // -------------------------------------------------------------------------

    [Fact]
    public void UcMyProfile_TxtUsername_IsReadOnly()
    {
        // Arrange
        var profile = new UcMyProfile();

        // Act — access the private txtUsername field via reflection
        var field = typeof(UcMyProfile)
            .GetField("txtUsername", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(field);

        var textBox = field.GetValue(profile) as System.Windows.Forms.TextBox;
        Assert.NotNull(textBox);

        // Assert
        Assert.True(textBox.ReadOnly, "txtUsername must be read-only so the customer cannot edit their username.");
    }

    // -------------------------------------------------------------------------
    // Requirement 1.7 — LoadProfileAsync sets txtUsername.Text to customer.Username
    // -------------------------------------------------------------------------

    [Fact]
    public void UcMyProfile_ApplyCustomerToUi_SetsTxtUsernameText()
    {
        // Arrange
        var profile = new UcMyProfile();

        var customer = new Customer
        {
            Id = 1,
            FullName = "Test User",
            Email = "test@example.com",
            Phone = "0900000001",
            Username = "test_username_42",
            PasswordHash = "hash",
            MemberCode = "CM-000001",
            Tier = "Standard",
            LoyaltyPoints = 0,
            MembershipPoints = 0,
            TotalPoints = 0,
            TotalSpent = 0,
            MonthlySpent = 0,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        // Act — call the internal helper that LoadProfileAsync delegates to
        profile.ApplyCustomerToUi(customer);

        // Assert — txtUsername.Text must equal the customer's username
        var field = typeof(UcMyProfile)
            .GetField("txtUsername", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(field);

        var textBox = field.GetValue(profile) as System.Windows.Forms.TextBox;
        Assert.NotNull(textBox);

        Assert.Equal(customer.Username, textBox.Text);
    }

    [Fact]
    public void UcMyProfile_ApplyCustomerToUi_UpdatesTxtUsernameWhenCustomerChanges()
    {
        // Arrange
        var profile = new UcMyProfile();

        var customerA = new Customer
        {
            Id = 1,
            FullName = "Alice",
            Email = "alice@example.com",
            Phone = "0900000001",
            Username = "alice_99",
            PasswordHash = "hash",
            MemberCode = "CM-000001",
            Tier = "Standard",
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        var customerB = new Customer
        {
            Id = 2,
            FullName = "Bob",
            Email = "bob@example.com",
            Phone = "0900000002",
            Username = "bob.smith",
            PasswordHash = "hash",
            MemberCode = "CM-000002",
            Tier = "VIP",
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        var field = typeof(UcMyProfile)
            .GetField("txtUsername", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);

        // Act — apply first customer
        profile.ApplyCustomerToUi(customerA);
        var textBox = field.GetValue(profile) as System.Windows.Forms.TextBox;
        Assert.NotNull(textBox);
        Assert.Equal("alice_99", textBox.Text);

        // Act — apply second customer
        profile.ApplyCustomerToUi(customerB);
        Assert.Equal("bob.smith", textBox.Text);
    }
}
