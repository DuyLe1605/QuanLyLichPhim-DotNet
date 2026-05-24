import { Link, useNavigate } from "react-router-dom";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useAuth } from "../hooks/useAuth";

const loginSchema = z.object({
  username: z.string().min(1, "Vui lòng nhập tên đăng nhập"),
  password: z.string().min(3, "Mật khẩu phải có ít nhất 3 ký tự")
});

type LoginForm = z.infer<typeof loginSchema>;

export function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting }
  } = useForm<LoginForm>({
    resolver: zodResolver(loginSchema)
  });

  async function submit(data: LoginForm) {
    try {
      await login(data);
      navigate("/profile");
    } catch (error) {
      alert("Đăng nhập thất bại. Sai thông tin hoặc tài khoản không tồn tại.");
    }
  }

  return (
    <form className="auth-card" onSubmit={handleSubmit(submit)}>
      <h1>Đăng nhập</h1>
      
      <div className="form-group">
        <label htmlFor="username">Tên đăng nhập</label>
        <input 
          id="username"
          placeholder="Nhập username" 
          {...register("username")} 
          className={errors.username ? "input-error" : ""}
        />
        {errors.username && <span className="error-message">{errors.username.message}</span>}
      </div>

      <div className="form-group">
        <label htmlFor="password">Mật khẩu</label>
        <input 
          id="password"
          placeholder="Nhập mật khẩu" 
          type="password" 
          {...register("password")} 
          className={errors.password ? "input-error" : ""}
        />
        {errors.password && <span className="error-message">{errors.password.message}</span>}
      </div>

      <button className="primary-button full" disabled={isSubmitting}>
        {isSubmitting ? "Đang xử lý..." : "Đăng nhập"}
      </button>
      
      <p className="auth-footer">
        Chưa có tài khoản? <Link to="/register">Đăng ký ngay</Link>
      </p>
    </form>
  );
}
