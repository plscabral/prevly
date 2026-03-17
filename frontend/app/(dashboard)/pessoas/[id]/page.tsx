"use client";

import { useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { ArrowLeft, Loader2, Pencil, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Field,
  FieldError,
  FieldGroup,
  FieldLabel,
} from "@/components/ui/field";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  getGetApiPersonIdQueryKey,
  getGetApiPersonQueryKey,
  putApiPersonIdResponseSuccess,
  useDeleteApiPersonId,
  usePutApiPersonId,
} from "@/lib/api/generated/person/person";
import type { Person } from "@/lib/api/generated/model";
import {
  getApiNitResponseSuccess,
  getGetApiNitQueryKey,
  postApiNitBindPersonResponseSuccess,
  useGetApiNit,
  usePostApiNitBindPerson,
} from "@/lib/api/generated/nit/nit";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { customFetch } from "@/lib/api/http-client";
import { cn } from "@/lib/utils";
import {
  getRetirementRequestStatusLabel,
  getRetirementRequestStatusStyle,
} from "@/lib/person-retirement-status";
import { toast } from "sonner";

interface FormErrors {
  name?: string;
  cpf?: string;
}

interface MonitoredEmailDto {
  id?: string | null;
  personId: string;
  subject?: string | null;
  from?: string | null;
  rawContent?: string | null;
  summary?: string | null;
  receivedAt: string;
  identifiedStatus?: number | null;
  identifiedStatusLabel?: string | null;
  extractedName?: string | null;
  extractedCpf?: string | null;
  messageUniqueId?: string | null;
  createdAt: string;
}

interface PersonDetailsDto {
  person: Person;
  monitoredEmails: MonitoredEmailDto[];
}

const getPersonDetailsQueryKey = (personId: string) => ["person-details", personId] as const;

const formatCpf = (value: string) => {
  const digits = value.replace(/\D/g, "").slice(0, 11);
  return digits
    .replace(/^(\d{3})(\d)/, "$1.$2")
    .replace(/^(\d{3})\.(\d{3})(\d)/, "$1.$2.$3")
    .replace(/\.(\d{3})(\d)/, ".$1-$2");
};

const formatWhatsApp = (value: string) => {
  const digits = value.replace(/\D/g, "").slice(0, 11);
  if (digits.length <= 2) return digits;
  if (digits.length <= 6) return `(${digits.slice(0, 2)}) ${digits.slice(2)}`;
  if (digits.length <= 10) {
    return `(${digits.slice(0, 2)}) ${digits.slice(2, 6)}-${digits.slice(6)}`;
  }
  return `(${digits.slice(0, 2)}) ${digits.slice(2, 7)}-${digits.slice(7)}`;
};

const toDateInputValue = (value?: string | null) => {
  if (!value) return "";
  const normalized = value.split("T")[0];
  return /^\d{4}-\d{2}-\d{2}$/.test(normalized) ? normalized : "";
};

const formatDateTime = (value?: string | null) => {
  if (!value) return "-";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "-";
  return date.toLocaleString("pt-BR");
};

export default function PessoaDetalhePage() {
  const params = useParams<{ id: string }>();
  const router = useRouter();
  const queryClient = useQueryClient();

  const personDetailsQuery = useQuery({
    queryKey: getPersonDetailsQueryKey(params.id),
    queryFn: async () => {
      const response = await customFetch<{ data: PersonDetailsDto }>(`/api/Person/${params.id}/details`);
      return response.data;
    },
  });

  const updateMutation = usePutApiPersonId();
  const deleteMutation = useDeleteApiPersonId();
  const bindPersonMutation = usePostApiNitBindPerson();

  const nitsQuery = useGetApiNit({
    PageNumber: 1,
    PageSize: 500,
  });

  const [isEditing, setIsEditing] = useState(false);
  const [errors, setErrors] = useState<FormErrors>({});
  const [selectedNitId, setSelectedNitId] = useState("");
  const [formData, setFormData] = useState({
    name: "",
    cpf: "",
    whatsApp: "",
    govPassword: "",
    birthDate: "",
  });

  const person = personDetailsQuery.data?.person;
  const monitoredEmails = personDetailsQuery.data?.monitoredEmails ?? [];

  useEffect(() => {
    if (!person) return;
    setFormData({
      name: person.name ?? "",
      cpf: person.cpf ?? "",
      whatsApp: person.whatsApp ?? "",
      govPassword: person.govPassword ?? "",
      birthDate: toDateInputValue(person.birthDate),
    });
    setSelectedNitId(person.nitId ?? "");
  }, [person]);

  const nitOptions = useMemo(() => {
    const fromApi = (
      (
        nitsQuery.data as
          | getApiNitResponseSuccess
          | undefined
      )?.data.data ?? []
    ).filter((nit) => nit.id && nit.number) as { id: string; number: string }[];

    if (
      selectedNitId &&
      !fromApi.some((nit) => nit.id === selectedNitId)
    ) {
      return [{ id: selectedNitId, number: `NIT atual (${selectedNitId})` }, ...fromApi];
    }

    return fromApi;
  }, [nitsQuery.data, selectedNitId]);

  const validate = () => {
    const nextErrors: FormErrors = {};
    if (!formData.name.trim()) nextErrors.name = "Nome é obrigatório";
    if (!formData.cpf.trim() || formData.cpf.replace(/\D/g, "").length !== 11) {
      nextErrors.cpf = "CPF inválido";
    }
    setErrors(nextErrors);
    return Object.keys(nextErrors).length === 0;
  };

  const handleCancelEdit = () => {
    if (!person) return;
    setFormData({
      name: person.name ?? "",
      cpf: person.cpf ?? "",
      whatsApp: person.whatsApp ?? "",
      govPassword: person.govPassword ?? "",
      birthDate: toDateInputValue(person.birthDate),
    });
    setSelectedNitId(person.nitId ?? "");
    setErrors({});
    setIsEditing(false);
  };

  const handleSave = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!person || !validate()) return;

    try {
      const updatedResponse = (await updateMutation.mutateAsync({
        id: params.id,
        data: {
          name: formData.name.trim(),
          cpf: formData.cpf.trim(),
          whatsApp: formData.whatsApp || undefined,
          govPassword: formData.govPassword || undefined,
          birthDate: formData.birthDate || undefined,
        },
      })) as putApiPersonIdResponseSuccess;

      if (selectedNitId && selectedNitId !== person.nitId) {
        await (bindPersonMutation.mutateAsync({
          data: {
            nitId: selectedNitId,
            personId: person.id!,
          },
        }) as Promise<postApiNitBindPersonResponseSuccess>);
      }

      const updatedPerson = updatedResponse.data;
      setFormData({
        name: updatedPerson.name ?? "",
        cpf: updatedPerson.cpf ?? "",
        whatsApp: updatedPerson.whatsApp ?? "",
        govPassword: updatedPerson.govPassword ?? "",
        birthDate: toDateInputValue(updatedPerson.birthDate),
      });

      await Promise.all([
        queryClient.invalidateQueries({ queryKey: getGetApiPersonQueryKey() }),
        queryClient.invalidateQueries({
          queryKey: getGetApiPersonIdQueryKey(params.id),
        }),
        queryClient.invalidateQueries({ queryKey: getGetApiNitQueryKey() }),
        queryClient.invalidateQueries({ queryKey: getPersonDetailsQueryKey(params.id) }),
      ]);

      setIsEditing(false);
      toast.success("Pessoa atualizada com sucesso.");
    } catch (error) {
      toast.error(
        error instanceof Error ? error.message : "Erro ao atualizar pessoa.",
      );
    }
  };

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

  if (personDetailsQuery.isLoading) {
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
          </div>
        </div>
      </div>
    );
  }

  if (personDetailsQuery.isError || !person) {
    return (
      <div className="space-y-4">
        <h1 className="text-2xl font-semibold">Pessoa não encontrada</h1>
        <Button asChild variant="outline">
          <Link href="/pessoas">Voltar</Link>
        </Button>
      </div>
    );
  }

  const personStatusStyle = getRetirementRequestStatusStyle(person.retirementRequestStatus);
  const isSaving = updateMutation.isPending || bindPersonMutation.isPending;

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
          <span
            className={cn(
              "mt-3 inline-flex items-center gap-2 rounded-full px-2 py-1 text-xs font-medium",
              personStatusStyle.bg,
              personStatusStyle.text,
            )}
          >
            <span className={cn("h-1.5 w-1.5 rounded-full", personStatusStyle.dot)} />
            {getRetirementRequestStatusLabel(person.retirementRequestStatus)}
          </span>
        </div>
        <div className="flex items-center gap-2">
          {!isEditing && (
            <Button variant="outline" className="gap-2" onClick={() => setIsEditing(true)}>
              <Pencil className="h-4 w-4" />
              Editar
            </Button>
          )}
          <Button
            variant="destructive"
            className="gap-2"
            onClick={handleDelete}
            disabled={deleteMutation.isPending || isSaving}
          >
            {deleteMutation.isPending ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              <Trash2 className="h-4 w-4" />
            )}
            Deletar
          </Button>
        </div>
      </div>

      <section className="rounded-lg border border-border bg-card p-6">
        <h2 className="text-base font-semibold text-foreground">Dados</h2>
        <div className="my-4 h-px w-full bg-border" />

        <form onSubmit={handleSave} className="space-y-5" autoComplete="off">
          <FieldGroup>
            <div className="grid gap-5 sm:grid-cols-2">
              <Field data-invalid={!!errors.name}>
                <FieldLabel htmlFor="name">Nome Completo</FieldLabel>
                <Input
                  id="name"
                  value={formData.name}
                  onChange={(event) =>
                    setFormData((prev) => ({
                      ...prev,
                      name: event.target.value,
                    }))
                  }
                  className="bg-background"
                  disabled={!isEditing || isSaving}
                />
                {errors.name && <FieldError>{errors.name}</FieldError>}
              </Field>
              <Field data-invalid={!!errors.cpf}>
                <FieldLabel htmlFor="cpf">CPF</FieldLabel>
                <Input
                  id="cpf"
                  value={formData.cpf}
                  placeholder="Ex: 123.456.789-00"
                  onChange={(event) =>
                    setFormData((prev) => ({
                      ...prev,
                      cpf: formatCpf(event.target.value),
                    }))
                  }
                  className="bg-background"
                  disabled={!isEditing || isSaving}
                />
                {errors.cpf && <FieldError>{errors.cpf}</FieldError>}
              </Field>
            </div>

            <div className="grid gap-5 sm:grid-cols-2">
              <Field>
                <FieldLabel htmlFor="birthDate">Data de Nascimento</FieldLabel>
                <Input
                  id="birthDate"
                  type="date"
                  value={formData.birthDate}
                  onChange={(event) =>
                    setFormData((prev) => ({
                      ...prev,
                      birthDate: event.target.value,
                    }))
                  }
                  className="bg-background"
                  disabled={!isEditing || isSaving}
                />
              </Field>
              <Field>
                <FieldLabel htmlFor="whatsApp">WhatsApp</FieldLabel>
                <Input
                  id="whatsApp"
                  value={formData.whatsApp}
                  placeholder="Ex: (11) 91234-5678"
                  onChange={(event) =>
                    setFormData((prev) => ({
                      ...prev,
                      whatsApp: formatWhatsApp(event.target.value),
                    }))
                  }
                  className="bg-background"
                  disabled={!isEditing || isSaving}
                />
              </Field>
            </div>

            <div className="grid gap-5 sm:grid-cols-2">
              <Field>
                <FieldLabel htmlFor="govPassword">Senha Gov.br</FieldLabel>
                <Input
                  id="govPassword"
                  autoComplete="new-password"
                  value={formData.govPassword}
                  onChange={(event) =>
                    setFormData((prev) => ({
                      ...prev,
                      govPassword: event.target.value,
                    }))
                  }
                  className="bg-background"
                  disabled={!isEditing || isSaving}
                />
              </Field>
              <Field>
                <FieldLabel>NIT para vincular</FieldLabel>
                <Select
                  value={selectedNitId}
                  onValueChange={setSelectedNitId}
                  disabled={!isEditing || isSaving}
                >
                  <SelectTrigger className="bg-background">
                    <SelectValue placeholder="Selecione um NIT pronto para vínculo" />
                  </SelectTrigger>
                  <SelectContent>
                    {nitOptions.map((nit) => (
                      <SelectItem key={nit.id} value={nit.id}>
                        {nit.number}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </Field>
            </div>
          </FieldGroup>

          {isEditing && (
            <div className="flex justify-end gap-3">
              <Button type="button" variant="outline" onClick={handleCancelEdit} disabled={isSaving}>
                Cancelar
              </Button>
              <Button type="submit" disabled={isSaving}>
                {isSaving ? (
                  <>
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    Salvando...
                  </>
                ) : (
                  "Salvar alterações"
                )}
              </Button>
            </div>
          )}
        </form>
      </section>

      <section className="rounded-lg border border-border bg-card p-6">
        <h2 className="text-base font-semibold text-foreground">Histórico de e-mails monitorados</h2>
        <div className="my-4 h-px w-full bg-border" />

        {!monitoredEmails.length ? (
          <p className="text-sm text-muted-foreground">Nenhum e-mail monitorado para esta pessoa.</p>
        ) : (
          <div className="space-y-4">
            {monitoredEmails.map((email) => {
              const emailStatusStyle = getRetirementRequestStatusStyle(email.identifiedStatus);

              return (
                <article
                  key={email.id ?? `${email.messageUniqueId}-${email.receivedAt}`}
                  className="rounded-lg border border-border bg-background p-4"
                >
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <h3 className="text-sm font-semibold text-foreground">{email.subject || "(sem assunto)"}</h3>
                    <span className="text-xs text-muted-foreground">{formatDateTime(email.receivedAt)}</span>
                  </div>

                  <div className="mt-2 flex flex-wrap items-center gap-3 text-xs text-muted-foreground">
                    <span><strong>Remetente:</strong> {email.from || "-"}</span>
                    <span
                      className={cn(
                        "inline-flex items-center gap-2 rounded-full px-2 py-1 font-medium",
                        emailStatusStyle.bg,
                        emailStatusStyle.text,
                      )}
                    >
                      <span className={cn("h-1.5 w-1.5 rounded-full", emailStatusStyle.dot)} />
                      {email.identifiedStatusLabel || getRetirementRequestStatusLabel(email.identifiedStatus)}
                    </span>
                  </div>

                  <div className="mt-3 grid gap-2 text-xs text-muted-foreground sm:grid-cols-2">
                    <p><strong>Nome extraído:</strong> {email.extractedName || "-"}</p>
                    <p><strong>CPF extraído:</strong> {email.extractedCpf || "-"}</p>
                  </div>

                  <div className="mt-3 rounded-md border border-border bg-card p-3">
                    <p className="text-xs font-medium text-foreground">Resumo</p>
                    <p className="mt-1 text-sm text-muted-foreground">{email.summary || "-"}</p>
                  </div>

                  <div className="mt-3 rounded-md border border-border bg-card p-3">
                    <p className="text-xs font-medium text-foreground">Conteúdo completo</p>
                    <pre className="mt-1 max-h-56 overflow-auto whitespace-pre-wrap text-xs text-muted-foreground">
                      {email.rawContent || "-"}
                    </pre>
                  </div>
                </article>
              );
            })}
          </div>
        )}
      </section>
    </div>
  );
}
