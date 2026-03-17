"use client";

import { createAuthClient } from "better-auth/react";
import { credentialsClient } from "better-auth-credentials-plugin/client";
import { authCredentialsSchema } from "@/lib/auth-credentials-schema";

export const authClient = createAuthClient({
  baseURL:
    process.env.NEXT_PUBLIC_BETTER_AUTH_URL ??
    process.env.BETTER_AUTH_URL ??
    "http://localhost:3000/api/auth",
  plugins: [
    credentialsClient<any, "/sign-in/credentials", typeof authCredentialsSchema>(),
  ],
});
