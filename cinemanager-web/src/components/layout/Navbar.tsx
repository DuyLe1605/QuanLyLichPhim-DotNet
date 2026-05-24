import { Film, LogOut, UserRound } from "lucide-react";
import { Link, NavLink } from "react-router-dom";
import { useAuth } from "../../hooks/useAuth";

export function Navbar() {
  const { customer, logout } = useAuth();

  return (
    <header className="navbar">
      <Link to="/" className="brand">
        <Film size={28} />
        <span>Star Cinema</span>
      </Link>
      <nav>
        <NavLink to="/movies">Phim</NavLink>
        <NavLink to="/about">Giới thiệu</NavLink>
        {customer && <NavLink to="/profile">Tài khoản</NavLink>}
      </nav>
      <div className="nav-actions">
        {customer ? (
          <>
            <span className="user-pill">
              <UserRound size={16} /> {customer.fullName}
            </span>
            <button className="icon-button" onClick={logout} title="Đăng xuất">
              <LogOut size={18} />
            </button>
          </>
        ) : (
          <>
            <Link className="primary-button" to="/auth">Đăng nhập/Đăng ký</Link>
          </>
        )}
      </div>
    </header>
  );
}
