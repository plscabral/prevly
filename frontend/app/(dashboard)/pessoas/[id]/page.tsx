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
  getApiPersonIdResponseSuccess,
  getGetApiPersonIdQueryKey,
  getGetApiPersonQueryKey,
  putApiPersonIdResponseSuccess,
  useDeleteApiPersonId,
  useGetApiPersonId,
  usePutApiPersonId,
} from "@/lib/api/generated/person/person";
import {
  getApiNitResponseSuccess,
  getGetApiNitQueryKey,
  postApiNitBindPersonResponseSuccess,
  useGetApiNit,
  usePostApiNitBindPerson,
} from "@/lib/api/generated/nit/nit";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

interface FormErrors {
  name?: string;
  cpf?: string;
}

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

export default function PessoaDetalhePage() {
  const params = useParams<{ id: string }>();
  const router = useRouter();
  const queryClient = useQueryClient();

  const personQuery = useGetApiPersonId(params.id);
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

  const personResponse = personQuery.data as
    | getApiPersonIdResponseSuccess
    | undefined;
  const person = personResponse?.data;

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
          </div>
        </div>
      </div>
    );
  }

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
    </div>
  );
}
