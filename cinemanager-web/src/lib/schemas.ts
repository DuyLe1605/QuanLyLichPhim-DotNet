import { z } from "zod";

export const loginSchema = z.object({
  username: z.string().min(3),
  password: z.string().min(6)
});

export const registerSchema = loginSchema.extend({
  fullName: z.string().min(2),
  email: z.string().email(),
  phone: z.string().regex(/^0\d{9}$/)
});
