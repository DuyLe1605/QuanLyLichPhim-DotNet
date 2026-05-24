import { createContext, useMemo, useState } from "react";
import { authApi } from "../api/auth.api";
import type { AuthResponse, Customer, LoginInput, RegisterInput } from "../lib/types";

type AuthContextValue = {
  customer: Customer | null;
  login: (input: LoginInput) => Promise<void>;
  register: (input: RegisterInput) => Promise<void>;
  logout: () => void;
};

export const AuthContext = createContext<AuthContextValue | null>(null);

const storedCustomer = () => {
  const raw = localStorage.getItem("cm_customer");
  return raw ? (JSON.parse(raw) as Customer) : null;
};

function persistAuth(response: AuthResponse) {
  localStorage.setItem("cm_access_token", response.accessToken);
  localStorage.setItem("cm_refresh_token", response.refreshToken);
  localStorage.setItem("cm_customer", JSON.stringify(response.customer));
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [customer, setCustomer] = useState<Customer | null>(storedCustomer);

  const value = useMemo<AuthContextValue>(
    () => ({
      customer,
      login: async (input) => {
        const response = await authApi.login(input);
        persistAuth(response);
        setCustomer(response.customer);
      },
      register: async (input) => {
        const response = await authApi.register(input);
        persistAuth(response);
        setCustomer(response.customer);
      },
      logout: () => {
        localStorage.removeItem("cm_access_token");
        localStorage.removeItem("cm_refresh_token");
        localStorage.removeItem("cm_customer");
        setCustomer(null);
      }
    }),
    [customer]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
