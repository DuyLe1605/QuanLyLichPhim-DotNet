using BaiTapLon.Models;

namespace BaiTapLon.Helpers;

/// <summary>
/// Quản lý phiên đăng nhập của user hiện tại (Singleton pattern).
/// Hỗ trợ cả User (Admin/Staff) và Customer.
/// </summary>
public static class SessionManager
{
    // ===== User (Admin/Staff) =====
    public static User? CurrentUser { get; private set; }

    public static bool IsLoggedIn => CurrentUser != null;
    public static bool IsAdmin => CurrentUser?.Role == "Admin";
    public static bool IsStaff => CurrentUser?.Role == "Staff";

    public static void Login(User user)
    {
        CurrentUser = user;
        CurrentCustomer = null; // Đảm bảo chỉ 1 phiên
    }

    // ===== Customer =====
    public static Customer? CurrentCustomer { get; private set; }

    public static bool IsCustomerLoggedIn => CurrentCustomer != null;

    public static void LoginAsCustomer(Customer customer)
    {
        CurrentCustomer = customer;
        CurrentUser = null; // Đảm bảo chỉ 1 phiên
    }

    // ===== Shared =====
    public static void Logout()
    {
        CurrentUser = null;
        CurrentCustomer = null;
    }
}
