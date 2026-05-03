using BaiTapLon.Models;

namespace BaiTapLon.Helpers;

/// <summary>
/// Quản lý phiên đăng nhập của user hiện tại (Singleton pattern).
/// </summary>
public static class SessionManager
{
    public static User? CurrentUser { get; private set; }

    public static bool IsLoggedIn => CurrentUser != null;
    public static bool IsAdmin => CurrentUser?.Role == "Admin";
    public static bool IsStaff => CurrentUser?.Role == "Staff";

    public static void Login(User user)
    {
        CurrentUser = user;
    }

    public static void Logout()
    {
        CurrentUser = null;
    }
}
