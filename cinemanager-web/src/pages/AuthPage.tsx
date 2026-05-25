import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useAuth } from "../hooks/useAuth";
import { useNavigate } from "react-router-dom";
import { useDialog } from "../hooks/useDialog";

const loginSchema = z.object({
  username: z.string().min(1, "Vui lòng nhập tên đăng nhập"),
  password: z.string().min(3, "Mật khẩu phải có ít nhất 3 ký tự")
});

const registerSchema = z.object({
  fullName: z.string().min(2, "Họ tên phải có ít nhất 2 ký tự"),
  username: z.string().min(3, "Tên đăng nhập phải có ít nhất 3 ký tự"),
  email: z.string().email("Email không hợp lệ"),
  phone: z.string().regex(/^(0|\+84)\d{9}$/, "Số điện thoại không hợp lệ"),
  password: z.string().min(6, "Mật khẩu phải từ 6 ký tự trở lên")
});

type LoginForm = z.infer<typeof loginSchema>;
type RegisterForm = z.infer<typeof registerSchema>;

export function AuthPage() {
  const { login, register: authRegister } = useAuth();
  const navigate = useNavigate();
  const dialog = useDialog();

  const loginForm = useForm<LoginForm>({ resolver: zodResolver(loginSchema) });
  const registerForm = useForm<RegisterForm>({ resolver: zodResolver(registerSchema) });

  async function onLoginSubmit(data: LoginForm) {
    try {
      await login(data);
      navigate("/profile");
    } catch (error) {
      await dialog.alert({
        variant: "error",
        title: "Đăng nhập thất bại",
        message: "Sai thông tin hoặc tài khoản không tồn tại."
      });
    }
  }

  async function onRegisterSubmit(data: RegisterForm) {
    try {
      await authRegister(data);
      navigate("/profile");
    } catch (error) {
      await dialog.alert({
        variant: "error",
        title: "Đăng ký thất bại",
        message: "Vui lòng kiểm tra lại thông tin."
      });
    }
  }

  return (
    <div className="auth-split page">
      {/* Cột Đăng nhập */}
      <div>
        <h1>Đăng nhập tài khoản</h1>
        <form onSubmit={loginForm.handleSubmit(onLoginSubmit)}>
          <div className="form-group">
            <label htmlFor="login-username">Tên đăng nhập *</label>
            <input
              id="login-username"
              placeholder="Tên đăng nhập"
              {...loginForm.register("username")}
              className={loginForm.formState.errors.username ? "input-error" : ""}
            />
            {loginForm.formState.errors.username && (
              <span className="error-message">{loginForm.formState.errors.username.message}</span>
            )}
          </div>

          <div className="form-group">
            <label htmlFor="login-password">Mật khẩu *</label>
            <input
              id="login-password"
              placeholder="Mật khẩu"
              type="password"
              {...loginForm.register("password")}
              className={loginForm.formState.errors.password ? "input-error" : ""}
            />
            {loginForm.formState.errors.password && (
              <span className="error-message">{loginForm.formState.errors.password.message}</span>
            )}
          </div>

          <button className="primary-button" disabled={loginForm.formState.isSubmitting} style={{ marginTop: '16px' }}>
            {loginForm.formState.isSubmitting ? "Đang xử lý..." : "ĐĂNG NHẬP"}
          </button>
        </form>
      </div>

      {/* Cột Đăng ký */}
      <div>
        <h1>Đăng ký tài khoản</h1>
        <form onSubmit={registerForm.handleSubmit(onRegisterSubmit)}>
          <div className="form-row">
            <div className="form-group">
              <label htmlFor="reg-fullName">Họ và tên *</label>
              <input
                id="reg-fullName"
                placeholder="Nguyễn Văn A"
                {...registerForm.register("fullName")}
                className={registerForm.formState.errors.fullName ? "input-error" : ""}
              />
              {registerForm.formState.errors.fullName && (
                <span className="error-message">{registerForm.formState.errors.fullName.message}</span>
              )}
            </div>
            
            <div className="form-group">
              <label htmlFor="reg-username">Tên đăng nhập *</label>
              <input
                id="reg-username"
                placeholder="Tên đăng nhập viết liền"
                {...registerForm.register("username")}
                className={registerForm.formState.errors.username ? "input-error" : ""}
              />
              {registerForm.formState.errors.username && (
                <span className="error-message">{registerForm.formState.errors.username.message}</span>
              )}
            </div>
          </div>

          <div className="form-row">
            <div className="form-group">
              <label htmlFor="reg-phone">Số điện thoại *</label>
              <input
                id="reg-phone"
                placeholder="0901234567"
                {...registerForm.register("phone")}
                className={registerForm.formState.errors.phone ? "input-error" : ""}
              />
              {registerForm.formState.errors.phone && (
                <span className="error-message">{registerForm.formState.errors.phone.message}</span>
              )}
            </div>
            
            <div className="form-group">
              <label htmlFor="reg-email">Email *</label>
              <input
                id="reg-email"
                type="email"
                placeholder="email@example.com"
                {...registerForm.register("email")}
                className={registerForm.formState.errors.email ? "input-error" : ""}
              />
              {registerForm.formState.errors.email && (
                <span className="error-message">{registerForm.formState.errors.email.message}</span>
              )}
            </div>
          </div>

          <div className="form-group">
            <label htmlFor="reg-password">Mật khẩu *</label>
            <input
              id="reg-password"
              type="password"
              placeholder="Mật khẩu"
              {...registerForm.register("password")}
              className={registerForm.formState.errors.password ? "input-error" : ""}
            />
            {registerForm.formState.errors.password && (
              <span className="error-message">{registerForm.formState.errors.password.message}</span>
            )}
          </div>

          <button className="primary-button" disabled={registerForm.formState.isSubmitting} style={{ marginTop: '16px' }}>
            {registerForm.formState.isSubmitting ? "Đang xử lý..." : "ĐĂNG KÝ"}
          </button>
        </form>
      </div>
    </div>
  );
}
