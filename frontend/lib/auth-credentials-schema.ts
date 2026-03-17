import { z } from "zod";

export const authCredentialsSchema = z.object({
  login: z.string().min(1, "Usuário é obrigatório"),
  password: z.string().min(1, "Senha é obrigatória"),
  rememberMe: z.boolean().optional(),
});
