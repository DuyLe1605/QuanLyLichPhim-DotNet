import { Link, useNavigate } from "react-router-dom";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useAuth } from "../hooks/useAuth";

const registerSchema = z.object({
  fullName: z.string().min(2, "Họ tên phải có ít nhất 2 ký tự"),
  username: z.string().min(3, "Tên đăng nhập phải có ít nhất 3 ký tự"),
  email: z.string().email("Email không hợp lệ"),
  phone: z.string().regex(/^(0|\+84)\d{9}$/, "Số điện thoại không hợp lệ"),
  password: z.string().min(6, "Mật khẩu phải từ 6 ký tự trở lên")
});

type RegisterForm = z.infer<typeof registerSchema>;

export function RegisterPage() {
  const { register: authRegister } = useAuth();
  const navigate = useNavigate();

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting }
  } = useForm<RegisterForm>({
    resolver: zodResolver(registerSchema)
  });

  async function submit(data: RegisterForm) {
    try {
      await authRegister(data);
      navigate("/profile");
    } catch (error) {
      alert("Đăng ký thất bại. Vui lòng kiểm tra lại thông tin.");
    }
  }

  return (
    <form className="auth-card register" onSubmit={handleSubmit(submit)}>
      <h1>Tạo tài khoản</h1>
      
      <div className="form-group">
        <label htmlFor="fullName">Họ và tên</label>
        <input 
          id="fullName" placeholder="Nguyễn Văn A" 
          {...register("fullName")} className={errors.fullName ? "input-error" : ""} 
        />
        {errors.fullName && <span className="error-message">{errors.fullName.message}</span>}
      </div>

      <div className="form-row">
        <div className="form-group">
          <label htmlFor="username">Tên đăng nhập</label>
          <input 
            id="username" placeholder="nguyenvana" 
            {...register("username")} className={errors.username ? "input-error" : ""} 
          />
          {errors.username && <span className="error-message">{errors.username.message}</span>}
        </div>

        <div className="form-group">
          <label htmlFor="phone">Số điện thoại</label>
          <input 
            id="phone" placeholder="0901234567" 
            {...register("phone")} className={errors.phone ? "input-error" : ""} 
          />
          {errors.phone && <span className="error-message">{errors.phone.message}</span>}
        </div>
      </div>

      <div className="form-group">
        <label htmlFor="email">Email</label>
        <input 
          id="email" type="email" placeholder="email@example.com" 
          {...register("email")} className={errors.email ? "input-error" : ""} 
        />
        {errors.email && <span className="error-message">{errors.email.message}</span>}
      </div>

      <div className="form-group">
        <label htmlFor="password">Mật khẩu</label>
        <input 
          id="password" type="password" placeholder="Tạo mật khẩu" 
          {...register("password")} className={errors.password ? "input-error" : ""} 
        />
        {errors.password && <span className="error-message">{errors.password.message}</span>}
      </div>

      <button className="primary-button full" disabled={isSubmitting}>
        {isSubmitting ? "Đang xử lý..." : "Đăng ký"}
      </button>

      <p className="auth-footer">
        Đã có tài khoản? <Link to="/login">Đăng nhập</Link>
      </p>
    </form>
  );
}
