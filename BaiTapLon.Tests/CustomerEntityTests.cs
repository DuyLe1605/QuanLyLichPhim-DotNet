using BaiTapLon.Data;
using BaiTapLon.Models;
using Microsoft.EntityFrameworkCore.Metadata;

namespace BaiTapLon.Tests;

/// <summary>
/// Unit tests for the Customer entity and AppDbContext model configuration.
/// Requirements: 1.1, 1.2
/// </summary>
public class CustomerEntityTests
{
    // -------------------------------------------------------------------------
    // Requirement 1.1 — Customer has a Username property of type string
    // -------------------------------------------------------------------------

    [Fact]
    public void Customer_HasUsernameProperty_OfTypeString()
    {
        var prop = typeof(Customer).GetProperty(nameof(Customer.Username));

        Assert.NotNull(prop);
        Assert.Equal(typeof(string), prop.PropertyType);
    }

    [Fact]
    public void Customer_UsernameDefaultValue_IsEmptyString()
    {
        var customer = new Customer();

        Assert.Equal(string.Empty, customer.Username);
    }

    [Fact]
    public void Customer_UsernameProperty_IsReadWrite()
    {
        var customer = new Customer { Username = "john_doe" };

        Assert.Equal("john_doe", customer.Username);
    }

    // -------------------------------------------------------------------------
    // Requirement 1.2 — AppDbContext has a unique index on Customer.Username
    // -------------------------------------------------------------------------

    [Fact]
    public void AppDbContext_CustomerUsername_HasUniqueIndex()
    {
        var (context, connection) = TestDbHelper.CreateSqliteContext();
        using (connection)
        using (context)
        {
            var entityType = context.Model.FindEntityType(typeof(Customer));
            Assert.NotNull(entityType);

            var indexes = entityType.GetIndexes();

            var uniqueUsernameIndex = indexes.FirstOrDefault(idx =>
                idx.IsUnique &&
                idx.Properties.Count == 1 &&
                idx.Properties[0].Name == nameof(Customer.Username));

            Assert.NotNull(uniqueUsernameIndex);
        }
    }

    [Fact]
    public void AppDbContext_CustomerUsername_MaxLengthIs50()
    {
        var (context, connection) = TestDbHelper.CreateSqliteContext();
        using (connection)
        using (context)
        {
            var entityType = context.Model.FindEntityType(typeof(Customer));
            Assert.NotNull(entityType);

            var usernameProp = entityType.FindProperty(nameof(Customer.Username));
            Assert.NotNull(usernameProp);
            Assert.Equal(50, usernameProp.GetMaxLength());
        }
    }
}
