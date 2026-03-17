"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { ArrowLeft, Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Field, FieldError, FieldGroup, FieldLabel } from "@/components/ui/field";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { toast } from "sonner";
import { useQueryClient } from "@tanstack/react-query";
import {
  getGetApiPersonQueryKey,
  postApiPersonResponseSuccess,
  usePostApiPerson,
} from "@/lib/api/generated/person/person";
import {
  getApiSocialSecurityRegistrationResponseSuccess,
  getGetApiSocialSecurityRegistrationQueryKey,
  useGetApiSocialSecurityRegistration,
  postApiSocialSecurityRegistrationBindPersonResponseSuccess,
  usePostApiSocialSecurityRegistrationBindPerson,
} from "@/lib/api/generated/social-security-registration/social-security-registration";
import { SocialSecurityRegistrationStatus } from "@/lib/api/generated/model";

interface FormErrors {
  name?: string;
  cpf?: string;
  age?: string;
}

export function PersonForm() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const createPersonMutation = usePostApiPerson();
  const bindPersonMutation = usePostApiSocialSecurityRegistrationBindPerson();
  const [errors, setErrors] = useState<FormErrors>({});
  const [selectedNitId, setSelectedNitId] = useState("");
  const [formData, setFormData] = useState({
    name: "",
    cpf: "",
    age: "",
    phone: "",
    whatsApp: "",
    govPassword: "",
    birthDate: "",
  });

  const nitsQuery = useGetApiSocialSecurityRegistration({
    PageNumber: 1,
    PageSize: 500,
    Status: SocialSecurityRegistrationStatus.NUMBER_4,
  });

  const nitOptions = useMemo(
    () =>
      (
        (nitsQuery.data as getApiSocialSecurityRegistrationResponseSuccess | undefined)?.data.data ?? []
      ).filter((nit) => nit.id && nit.number),
    [nitsQuery.data],
  );

  const validate = () => {
    const nextErrors: FormErrors = {};
    if (!formData.name.trim()) nextErrors.name = "Nome é obrigatório";
    if (!formData.cpf.trim() || formData.cpf.replace(/\D/g, "").length !== 11) {
      nextErrors.cpf = "CPF inválido";
    }
    if (!formData.age.trim()) nextErrors.age = "Idade é obrigatória";
    setErrors(nextErrors);
    return Object.keys(nextErrors).length === 0;
  };

  const onSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!validate()) return;

    try {
      const createdResponse = (await createPersonMutation.mutateAsync({
        data: {
          name: formData.name.trim(),
          cpf: formData.cpf.trim(),
          age: Number(formData.age),
          phone: formData.phone || undefined,
          whatsApp: formData.whatsApp || undefined,
          govPassword: formData.govPassword || undefined,
          birthDate: formData.birthDate || undefined,
        },
      })) as postApiPersonResponseSuccess;

      const createdPerson = createdResponse.data;
      if (!createdPerson.id) {
        throw new Error("Pessoa criada sem identificador.");
      }

      if (selectedNitId) {
        await (bindPersonMutation.mutateAsync({
          data: {
            socialSecurityRegistrationId: selectedNitId,
            personId: createdPerson.id,
          },
        }) as Promise<postApiSocialSecurityRegistrationBindPersonResponseSuccess>);
      }

      await Promise.all([
        queryClient.invalidateQueries({ queryKey: getGetApiPersonQueryKey() }),
        queryClient.invalidateQueries({
          queryKey: getGetApiSocialSecurityRegistrationQueryKey(),
        }),
      ]);

      toast.success("Pessoa cadastrada com sucesso!");
      router.push("/pessoas");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Erro ao cadastrar pessoa.");
    }
  };

  const isSubmitting = createPersonMutation.isPending || bindPersonMutation.isPending;

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Link
          href="/pessoas"
          className="mb-4 inline-flex items-center gap-1.5 text-sm text-muted-foreground hover:text-foreground"
        >
          <ArrowLeft className="h-4 w-4" />
          Voltar para Pessoas
        </Link>
        <h1 className="text-2xl font-semibold tracking-tight text-foreground">Nova Pessoa</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Preencha as informações para cadastrar uma nova pessoa.
        </p>
      </div>

      <div className="rounded-lg border border-border bg-card p-6">
        <form onSubmit={onSubmit} className="space-y-5">
          <FieldGroup>
            <Field data-invalid={!!errors.name}>
              <FieldLabel htmlFor="name">Nome Completo</FieldLabel>
              <Input
                id="name"
                value={formData.name}
                onChange={(event) => setFormData((prev) => ({ ...prev, name: event.target.value }))}
                className="bg-background"
              />
              {errors.name && <FieldError>{errors.name}</FieldError>}
            </Field>

            <div className="grid gap-5 sm:grid-cols-2">
              <Field data-invalid={!!errors.cpf}>
                <FieldLabel htmlFor="cpf">CPF</FieldLabel>
                <Input
                  id="cpf"
                  value={formData.cpf}
                  onChange={(event) => setFormData((prev) => ({ ...prev, cpf: event.target.value }))}
                  className="bg-background"
                />
                {errors.cpf && <FieldError>{errors.cpf}</FieldError>}
              </Field>
              <Field data-invalid={!!errors.age}>
                <FieldLabel htmlFor="age">Idade</FieldLabel>
                <Input
                  id="age"
                  value={formData.age}
                  onChange={(event) => setFormData((prev) => ({ ...prev, age: event.target.value.replace(/\D/g, "") }))}
                  className="bg-background"
                />
                {errors.age && <FieldError>{errors.age}</FieldError>}
              </Field>
            </div>

            <div className="grid gap-5 sm:grid-cols-2">
              <Field>
                <FieldLabel htmlFor="phone">Telefone</FieldLabel>
                <Input
                  id="phone"
                  value={formData.phone}
                  onChange={(event) => setFormData((prev) => ({ ...prev, phone: event.target.value }))}
                  className="bg-background"
                />
              </Field>
              <Field>
                <FieldLabel htmlFor="whatsApp">WhatsApp</FieldLabel>
                <Input
                  id="whatsApp"
                  value={formData.whatsApp}
                  onChange={(event) => setFormData((prev) => ({ ...prev, whatsApp: event.target.value }))}
                  className="bg-background"
                />
              </Field>
            </div>

            <div className="grid gap-5 sm:grid-cols-2">
              <Field>
                <FieldLabel htmlFor="govPassword">Senha Gov.br</FieldLabel>
                <Input
                  id="govPassword"
                  value={formData.govPassword}
                  onChange={(event) =>
                    setFormData((prev) => ({ ...prev, govPassword: event.target.value }))
                  }
                  className="bg-background"
                />
              </Field>
              <Field>
                <FieldLabel htmlFor="birthDate">Data de Nascimento</FieldLabel>
                <Input
                  id="birthDate"
                  type="date"
                  value={formData.birthDate}
                  onChange={(event) =>
                    setFormData((prev) => ({ ...prev, birthDate: event.target.value }))
                  }
                  className="bg-background"
                />
              </Field>
            </div>

            <Field>
              <FieldLabel>NIT para vincular (opcional)</FieldLabel>
              <Select value={selectedNitId} onValueChange={setSelectedNitId}>
                <SelectTrigger className="bg-background">
                  <SelectValue placeholder="Selecione um NIT pronto para vínculo" />
                </SelectTrigger>
                <SelectContent>
                  {nitOptions.map((nit) => (
                    <SelectItem key={nit.id} value={nit.id!}>
                      {nit.number}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </Field>
          </FieldGroup>

          <div className="flex justify-end gap-3">
            <Button type="button" variant="outline" asChild>
              <Link href="/pessoas">Cancelar</Link>
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Salvando...
                </>
              ) : (
                "Salvar pessoa"
              )}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
