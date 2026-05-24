import { Film, LogOut, UserRound } from "lucide-react";
import { Link, NavLink } from "react-router-dom";
import { useAuth } from "../../hooks/useAuth";

export function Navbar() {
  const { customer, logout } = useAuth();

  return (
    <header className="navbar">
      <Link to="/" className="brand">
        <Film size={28} />
        <span>CineManager</span>
      </Link>
      <nav>
        <NavLink to="/movies">Phim</NavLink>
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
            <Link className="ghost-button" to="/login">Đăng nhập</Link>
            <Link className="primary-button" to="/register">Đăng ký</Link>
          </>
        )}
      </div>
    </header>
  );
}
