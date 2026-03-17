import { betterAuth } from "better-auth";
import { nextCookies } from "better-auth/next-js";
import { credentials } from "better-auth-credentials-plugin";
import { APIError } from "better-auth/api";
import { authCredentialsSchema } from "@/lib/auth-credentials-schema";

const BACKEND_API_URL =
  process.env.BACKEND_API_URL ??
  process.env.NEXT_PUBLIC_API_BASE_URL ??
  "http://localhost:5085";

export const auth = betterAuth({
  baseURL:
    process.env.BETTER_AUTH_URL ??
    process.env.NEXT_PUBLIC_BETTER_AUTH_URL?.replace("/api/auth", "") ??
    "http://localhost:3000",
  basePath: "/api/auth",
  secret:
    process.env.BETTER_AUTH_SECRET ??
    "prevly-super-secret-change-in-production",
  database: undefined,
  session: {
    cookieCache: {
      enabled: true,
      strategy: "jwt",
    },
  },
  user: {
    additionalFields: {
      login: {
        type: "string",
        required: false,
        returned: true,
      },
      token: {
        type: "string",
        required: false,
        returned: true,
      },
    },
  },
  emailAndPassword: {
    enabled: false,
  },
  plugins: [
    nextCookies(),
    credentials({
      inputSchema: authCredentialsSchema,
      autoSignUp: true,
      async callback(_ctx, parsed) {
        const response = await fetch(`${BACKEND_API_URL}/api/Auth`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            login: parsed.login,
            password: parsed.password,
          }),
        });

        if (!response.ok) {
          throw new APIError("UNAUTHORIZED", {
            message: "Usuário ou senha inválidos.",
          });
        }

        const payload = (await response.json()) as {
          token: string;
          name: string;
          login: string;
        };

        return {
          email: `${payload.login}@prevly.local`,
          name: payload.name,
          login: payload.login,
          token: payload.token,
        };
      },
    }),
  ],
});
