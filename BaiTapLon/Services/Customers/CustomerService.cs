using Microsoft.EntityFrameworkCore;
using BaiTapLon.Data;
using BaiTapLon.Models;

namespace BaiTapLon.Services;

public class CustomerService
{
    private readonly AppDbContext _context;

    public CustomerService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Đăng ký khách hàng mới. Sinh mã thành viên tự động.
    /// </summary>
    public async Task<(bool Success, string Message, Customer? Customer)> RegisterAsync(
        string fullName, string email, string phone, string password)
    {
        // Validate email trùng
        if (await _context.Customers.AnyAsync(c => c.Email == email))
            return (false, "Email đã được đăng ký!", null);

        // Validate SĐT trùng
        if (await _context.Customers.AnyAsync(c => c.Phone == phone))
            return (false, "Số điện thoại đã được đăng ký!", null);

        var memberCode = await GenerateMemberCodeAsync();

        var customer = new Customer
        {
            FullName = fullName,
            Email = email,
            Phone = phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            MemberCode = memberCode,
            Tier = "Standard",
            TotalPoints = 0,
            TotalSpent = 0,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        return (true, $"Đăng ký thành công! Mã thành viên: {memberCode}", customer);
    }

    /// <summary>
    /// Đăng nhập khách hàng bằng email + password.
    /// </summary>
    public async Task<Customer?> LoginAsync(string email, string password)
    {
        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Email == email && c.IsActive);

        if (customer == null) return null;

        if (!BCrypt.Net.BCrypt.Verify(password, customer.PasswordHash))
            return null;

        return customer;
    }

    /// <summary>
    /// Lấy khách hàng theo ID.
    /// </summary>
    public async Task<Customer?> GetByIdAsync(int id)
    {
        return await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    /// <summary>
    /// Tìm khách hàng theo số điện thoại (Staff dùng khi bán vé).
    /// </summary>
    public async Task<Customer?> GetByPhoneAsync(string phone)
    {
        return await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Phone == phone && c.IsActive);
    }

    /// <summary>
    /// Tìm khách hàng theo mã thành viên (Staff quét barcode/QR).
    /// </summary>
    public async Task<Customer?> GetByMemberCodeAsync(string memberCode)
    {
        return await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.MemberCode == memberCode && c.IsActive);
    }

    /// <summary>
    /// Lấy tất cả khách hàng (Admin quản lý).
    /// </summary>
    public async Task<List<Customer>> GetAllAsync()
    {
        return await _context.Customers
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Tìm kiếm khách hàng theo tên, SĐT, hoặc email.
    /// </summary>
    public async Task<List<Customer>> SearchAsync(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return await GetAllAsync();

        keyword = keyword.Trim().ToLower();
        return await _context.Customers
            .AsNoTracking()
            .Where(c => c.FullName.ToLower().Contains(keyword)
                     || c.Phone.Contains(keyword)
                     || c.Email.ToLower().Contains(keyword)
                     || c.MemberCode.ToLower().Contains(keyword))
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Lọc theo hạng thành viên.
    /// </summary>
    public async Task<List<Customer>> GetByTierAsync(string tier)
    {
        return await _context.Customers
            .AsNoTracking()
            .Where(c => c.Tier == tier && c.IsActive)
            .OrderByDescending(c => c.TotalSpent)
            .ToListAsync();
    }

    /// <summary>
    /// Cập nhật hạng thành viên dựa trên TotalSpent.
    /// Standard: < 2,000,000đ | VIP: 2,000,000 - 10,000,000đ | Diamond: > 10,000,000đ
    /// </summary>
    public async Task UpdateTierAsync(int customerId)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null) return;

        string newTier = customer.TotalSpent switch
        {
            >= 10_000_000m => "Diamond",
            >= 2_000_000m => "VIP",
            _ => "Standard"
        };

        if (customer.Tier != newTier)
        {
            customer.Tier = newTier;
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Cộng dồn chi tiêu sau khi thanh toán.
    /// </summary>
    public async Task AddSpendingAsync(int customerId, decimal amount)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null) return;

        customer.TotalSpent += amount;
        await _context.SaveChangesAsync();

        // Tự động kiểm tra nâng hạng
        await UpdateTierAsync(customerId);
    }

    /// <summary>
    /// Cập nhật thông tin khách hàng.
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateProfileAsync(
        int customerId, string fullName, string phone)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null)
            return (false, "Không tìm thấy khách hàng!");

        // Kiểm tra SĐT trùng (trừ bản thân)
        if (await _context.Customers.AnyAsync(c => c.Phone == phone && c.Id != customerId))
            return (false, "Số điện thoại đã được sử dụng!");

        customer.FullName = fullName;
        customer.Phone = phone;
        await _context.SaveChangesAsync();

        return (true, "Cập nhật thành công!");
    }

    /// <summary>
    /// Đổi mật khẩu khách hàng.
    /// </summary>
    public async Task<(bool Success, string Message)> ChangePasswordAsync(
        int customerId, string currentPassword, string newPassword)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null)
            return (false, "Không tìm thấy khách hàng!");

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, customer.PasswordHash))
            return (false, "Mật khẩu hiện tại không đúng!");

        customer.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _context.SaveChangesAsync();

        return (true, "Đổi mật khẩu thành công!");
    }

    /// <summary>
    /// Vô hiệu hóa / kích hoạt tài khoản (Admin dùng).
    /// </summary>
    public async Task ToggleActiveAsync(int customerId)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null) return;

        customer.IsActive = !customer.IsActive;
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Sinh mã thành viên duy nhất dạng CM-XXXXXX.
    /// </summary>
    private async Task<string> GenerateMemberCodeAsync()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        string code;

        do
        {
            code = "CM-" + new string(Enumerable.Range(0, 6)
                .Select(_ => chars[random.Next(chars.Length)]).ToArray());
        }
        while (await _context.Customers.AnyAsync(c => c.MemberCode == code));

        return code;
    }
}
