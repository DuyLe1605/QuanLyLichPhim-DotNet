// Feature: customer-ui-improvements, Property 1
using System.Text.RegularExpressions;
using FsCheck;
using FsCheck.Xunit;
using BaiTapLon.Services;
using Xunit;

namespace BaiTapLon.Tests;

/// <summary>
/// Property-based tests for CustomerService.
/// </summary>
public class CustomerServicePropertyTests
{
    /// <summary>
    /// Property 1: Username validation accepts only valid identifiers.
    /// Validates: Requirements 1.4
    ///
    /// For any arbitrary string s, IsValidUsername(s) must equal true
    /// if and only if s matches ^[a-zA-Z0-9_.]{3,50}$.
    /// </summary>
    [Property]
    public bool IsValidUsername_MatchesRegex_ForArbitraryStrings(string s)
    {
        // The oracle: what the result should be
        bool expected = s != null && Regex.IsMatch(s, @"^[a-zA-Z0-9_.]{3,50}$");

        // The implementation under test
        bool actual = CustomerService.IsValidUsername(s!);

        return actual == expected;
    }

    // Feature: customer-ui-improvements, Property 2
    /// <summary>
    /// Property 2: Duplicate username registration is rejected.
    /// Validates: Requirements 1.5
    ///
    /// For any username that already exists in the customer table, calling RegisterAsync
    /// with that username SHALL return Success == false with a non-empty error message,
    /// regardless of the other registration fields.
    /// </summary>
    [Fact]
    public async Task RegisterAsync_RejectsDuplicateUsername()
    {
        // Arrange: create an isolated in-memory SQLite context
        var (context, connection) = TestDbHelper.CreateSqliteContext();
        await using (connection)
        await using (context)
        {
            var service = new CustomerService(context);

            const string sharedUsername = "user_1";

            // Register the first customer successfully
            var first = await service.RegisterAsync(
                fullName: "First User",
                email: "first@example.com",
                phone: "0900000001",
                password: "Password1!",
                username: sharedUsername);

            Assert.True(first.Success, $"First registration should succeed but got: {first.Message}");

            // Attempt to register a second customer with the SAME username but different fields
            var second = await service.RegisterAsync(
                fullName: "Second User",
                email: "second@example.com",
                phone: "0900000002",
                password: "Password2!",
                username: sharedUsername);

            // Assert: duplicate username must be rejected
            Assert.False(second.Success, "Second registration with duplicate username should fail.");
            Assert.False(string.IsNullOrEmpty(second.Message),
                "Error message must be non-empty when duplicate username is rejected.");
        }
    }

    // Feature: customer-ui-improvements, Property 2: Duplicate username registration is rejected
    // Validates: Requirements 1.5
    [Fact]
    public async Task RegisterAsync_RejectsDuplicateUsername_AliceAndBob()
    {
        // Arrange: create an isolated in-memory SQLite context
        var (context, connection) = TestDbHelper.CreateSqliteContext();
        await using (connection)
        await using (context)
        {
            var service = new CustomerService(context);

            // Register the first customer successfully
            var first = await service.RegisterAsync("Alice", "alice@test.com", "0901111111", "pass123", "alice_user");

            Assert.True(first.Success, $"First registration should succeed but got: {first.Message}");

            // Attempt to register a second customer with the SAME username but different email/phone
            var second = await service.RegisterAsync("Bob", "bob@test.com", "0902222222", "pass456", "alice_user");

            // Assert: duplicate username must be rejected
            Assert.False(second.Success, "Second registration with duplicate username should fail.");
            Assert.False(string.IsNullOrEmpty(second.Message),
                "Error message must be non-empty when duplicate username is rejected.");
        }
    }

    // Feature: customer-ui-improvements, Property 3: Login by username round-trip
    // Validates: Requirements 1.6
    [Fact]
    public async Task LoginAsync_RoundTrip_ReturnsCorrectCustomerOrNull()
    {
        // Arrange: create an isolated in-memory SQLite context
        var (context, connection) = TestDbHelper.CreateSqliteContext();
        await using (connection)
        await using (context)
        {
            var service = new CustomerService(context);

            // Register a customer
            var registration = await service.RegisterAsync(
                fullName: "Test User",
                email: "test@example.com",
                phone: "0901234567",
                password: "password123",
                username: "test_user");

            Assert.True(registration.Success, $"Registration should succeed but got: {registration.Message}");

            // Assert: correct username + correct password returns the customer
            var loginSuccess = await service.LoginAsync("test_user", "password123");
            Assert.NotNull(loginSuccess);
            Assert.Equal("test_user", loginSuccess.Username);

            // Assert: wrong username returns null
            var loginWrongUser = await service.LoginAsync("other_user", "password123");
            Assert.Null(loginWrongUser);

            // Assert: correct username + wrong password returns null
            var loginWrongPassword = await service.LoginAsync("test_user", "wrongpassword");
            Assert.Null(loginWrongPassword);
        }
    }
}
