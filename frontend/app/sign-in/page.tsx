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
    <div className="relative min-h-screen overflow-hidden bg-background">
      <div className="pointer-events-none absolute inset-0 bg-[radial-gradient(circle_at_top_right,hsl(var(--primary)/0.2),transparent_55%),radial-gradient(circle_at_bottom_left,hsl(var(--primary)/0.14),transparent_45%)]" />

      <main className="relative mx-auto flex min-h-screen w-full max-w-5xl items-center px-4 py-8 sm:px-6 lg:px-8">
        <div className="grid w-full gap-6 overflow-hidden rounded-3xl border border-border/70 bg-card/80 p-4 shadow-xl backdrop-blur sm:p-8 lg:min-h-[620px] lg:grid-cols-[1.1fr_0.9fr] lg:gap-10">
          <section className="hidden rounded-2xl bg-gradient-to-br from-primary/15 via-primary/5 to-transparent p-8 lg:flex lg:flex-col">
            <div className="mt-auto">
              <div className="inline-flex items-center gap-3 py-2">
                <div className="flex h-7 w-7 items-center justify-center rounded-md bg-foreground">
                  <span className="text-xs font-bold text-background">P</span>
                </div>
                <span className="text-base font-semibold tracking-tight text-foreground">
                  Prevly
                </span>
              </div>
              <h2 className="mt-6 max-w-sm text-3xl font-semibold tracking-tight text-foreground">
                Gestão previdenciária com mais clareza e velocidade.
              </h2>
              <p className="mt-3 max-w-sm text-sm leading-relaxed text-muted-foreground">
                Centralize pessoas, NITs e processos em um único lugar para
                manter seu time focado no que importa.
              </p>
              <ul className="mt-16 max-w-sm list-disc space-y-1.5 pl-4 text-xs text-foreground marker:text-foreground/80">
                <li>Organização de casos em tempo real</li>
                <li>Controle de NITs e vinculações sem planilhas</li>
                <li>Fluxo mais rápido para o seu time</li>
              </ul>
            </div>
          </section>

          <section className="mx-auto flex w-full max-w-md flex-col justify-center">
            <div className="mb-6 flex items-center gap-3 lg:hidden">
              <div className="flex h-8 w-8 items-center justify-center rounded-md bg-foreground">
                <span className="text-xs font-bold text-background">P</span>
              </div>
              <span className="text-lg font-semibold tracking-tight text-foreground">
                Prevly
              </span>
            </div>

            <h1 className="text-3xl font-semibold tracking-tight text-foreground">
              Entrar
            </h1>
            <p className="mt-2 text-sm text-muted-foreground">
              Acesse sua conta para continuar o acompanhamento dos casos.
            </p>

            <form onSubmit={handleSubmit} className="mt-7 space-y-4">
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
                  className="h-11"
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
                  className="h-11"
                />
              </div>

              <Button
                type="submit"
                className="h-11 w-full"
                disabled={isSubmitting}
              >
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
          </section>
        </div>
      </main>
    </div>
  );
}
