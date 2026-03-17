"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { ArrowLeft, Loader2, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import {
  getApiPersonIdResponseSuccess,
  getGetApiPersonQueryKey,
  useDeleteApiPersonId,
  useGetApiPersonId,
} from "@/lib/api/generated/person/person";
import { toast } from "sonner";
import { useQueryClient } from "@tanstack/react-query";

function ReadonlyField({
  label,
  value,
}: {
  label: string;
  value?: string | number | null;
}) {
  const displayValue =
    value === undefined || value === null || `${value}`.trim() === ""
      ? "Não informado"
      : `${value}`;
  return (
    <div className="space-y-2">
      <p className="text-sm text-foreground">{label}</p>
      <div className="rounded-md border border-border bg-muted/30 px-3 py-2 text-sm text-muted-foreground">
        {displayValue}
      </div>
    </div>
  );
}

export default function PessoaDetalhePage() {
  const params = useParams<{ id: string }>();
  const router = useRouter();
  const queryClient = useQueryClient();
  const personQuery = useGetApiPersonId(params.id);
  const deleteMutation = useDeleteApiPersonId();

  const handleDelete = async () => {
    try {
      await deleteMutation.mutateAsync({ id: params.id });
      await queryClient.invalidateQueries({
        queryKey: getGetApiPersonQueryKey(),
      });
      toast.success("Pessoa removida.");
      router.push("/pessoas");
    } catch (error) {
      toast.error(
        error instanceof Error ? error.message : "Erro ao remover pessoa.",
      );
    }
  };

  if (personQuery.isLoading) {
    return (
      <div className="flex flex-col gap-6">
        <div className="space-y-2">
          <Skeleton className="h-5 w-36" />
          <Skeleton className="h-8 w-64" />
          <Skeleton className="h-4 w-40" />
        </div>
        <div className="rounded-lg border border-border bg-card p-6">
          <Skeleton className="h-5 w-24" />
          <div className="my-4 h-px w-full bg-border" />
          <div className="grid gap-5 sm:grid-cols-2">
            <Skeleton className="h-16 w-full" />
            <Skeleton className="h-16 w-full" />
            <Skeleton className="h-16 w-full" />
            <Skeleton className="h-16 w-full" />
            <Skeleton className="h-16 w-full" />
            <Skeleton className="h-16 w-full" />
            <Skeleton className="h-16 w-full" />
            <Skeleton className="h-16 w-full" />
          </div>
        </div>
      </div>
    );
  }

  const personResponse = personQuery.data as
    | getApiPersonIdResponseSuccess
    | undefined;
  const person = personResponse?.data;

  if (personQuery.isError || !person) {
    return (
      <div className="space-y-4">
        <h1 className="text-2xl font-semibold">Pessoa não encontrada</h1>
        <Button asChild variant="outline">
          <Link href="/pessoas">Voltar</Link>
        </Button>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-start justify-between gap-3">
        <div>
          <Link
            href="/pessoas"
            className="mb-4 inline-flex items-center gap-1.5 text-sm text-muted-foreground transition-colors hover:text-foreground"
          >
            <ArrowLeft className="h-4 w-4" />
            Voltar
          </Link>
          <h1 className="text-2xl font-semibold tracking-tight text-foreground">
            {person.name}
          </h1>
          <p className="mt-1 text-sm text-muted-foreground">CPF {person.cpf}</p>
        </div>
        <Button
          variant="destructive"
          className="gap-2"
          onClick={handleDelete}
          disabled={deleteMutation.isPending}
        >
          {deleteMutation.isPending ? (
            <Loader2 className="h-4 w-4 animate-spin" />
          ) : (
            <Trash2 className="h-4 w-4" />
          )}
          Deletar
        </Button>
      </div>

      <section className="rounded-lg border border-border bg-card p-6">
        <h2 className="text-base font-semibold text-foreground">Dados</h2>
        <div className="my-4 h-px w-full bg-border" />
        <div className="grid gap-5 sm:grid-cols-2">
          <ReadonlyField label="Nome" value={person.name} />
          <ReadonlyField label="CPF" value={person.cpf} />
          <ReadonlyField label="Telefone" value={person.phone} />
          <ReadonlyField label="WhatsApp" value={person.whatsApp} />
          <ReadonlyField
            label="Idade"
            value={person.age ? `${person.age} anos` : null}
          />
          <ReadonlyField label="Senha Gov.br" value={person.govPassword} />
          <ReadonlyField
            label="Data de nascimento"
            value={
              person.birthDate
                ? new Date(person.birthDate).toLocaleDateString("pt-BR")
                : null
            }
          />
          <ReadonlyField
            label="NIT vinculado"
            value={person.socialSecurityRegistrationId}
          />
        </div>
      </section>
    </div>
  );
}
