"use client";

import { FormEvent, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { Loader2 } from "lucide-react";
import { authClient } from "@/lib/auth-client";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { toast } from "sonner";

const TOKEN_STORAGE_KEY = "prevly_token";

export default function SignInPage() {
  const router = useRouter();
  const [redirectTo, setRedirectTo] = useState("/");
  const [login, setLogin] = useState("");
  const [password, setPassword] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const session = authClient.useSession();

  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    setRedirectTo(params.get("redirectTo") || "/");
  }, []);

  useEffect(() => {
    if (session.data) {
      const user = session.data.user as { token?: string } | undefined;
      if (user?.token) {
        window.localStorage.setItem(TOKEN_STORAGE_KEY, user.token);
      }
      router.replace(redirectTo);
    }
  }, [session.data, redirectTo, router]);

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setIsSubmitting(true);

    try {
      const result = await authClient.signIn.credentials({
        login,
        password,
      });

      if (result.error) {
        throw new Error(result.error.message || "Falha na autenticação.");
      }

      const sessionData = await authClient.getSession();
      const user = sessionData.data?.user as { token?: string } | undefined;
      if (user?.token) {
        window.localStorage.setItem(TOKEN_STORAGE_KEY, user.token);
      }

      router.replace(redirectTo);
      toast.success("Login realizado com sucesso.");
    } catch (error) {
      toast.error(
        error instanceof Error ? error.message : "Erro ao autenticar.",
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-background px-6">
      <div className="w-full max-w-md rounded-xl border border-border bg-card p-6 shadow-sm">
        <h1 className="text-2xl font-semibold tracking-tight text-foreground">
          Entrar
        </h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Use seu usuário e senha para acessar.
        </p>

        <form onSubmit={handleSubmit} className="mt-6 space-y-4">
          <div className="space-y-2">
            <label htmlFor="login" className="text-sm text-foreground">
              Usuário
            </label>
            <Input
              id="login"
              value={login}
              onChange={(event) => setLogin(event.target.value)}
              autoComplete="username"
              required
            />
          </div>

          <div className="space-y-2">
            <label htmlFor="password" className="text-sm text-foreground">
              Senha
            </label>
            <Input
              id="password"
              type="password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              autoComplete="current-password"
              required
            />
          </div>

          <Button type="submit" className="w-full" disabled={isSubmitting}>
            {isSubmitting ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                Entrando...
              </>
            ) : (
              "Entrar"
            )}
          </Button>
        </form>
      </div>
    </div>
  );
}
