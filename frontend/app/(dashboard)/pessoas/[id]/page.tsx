"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import Link from "next/link";
import { useParams, useRouter, useSearchParams } from "next/navigation";
import {
  ArrowLeft,
  Check,
  Copy,
  Download,
  Eye,
  EyeOff,
  FileText,
  FolderOpen,
  HandCoins,
  Loader2,
  Pencil,
  Trash2,
  Upload,
  UserRound,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
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
  getRetirementRequestStatusLabelFromApi,
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
  retirementAgreement?: PersonRetirementAgreementDto | null;
  financialEntries: PersonFinancialEntryDto[];
  documents: PersonDocumentDto[];
  financialSummary?: PersonFinancialSummaryDto | null;
}

interface PersonOperationalCostItemDto {
  id?: string | null;
  description?: string | null;
  value: number;
}

interface PersonRetirementAgreementDto {
  totalCost?: number | null;
  operationalCostType: number;
  operationalCostSimpleValue?: number | null;
  operationalCostItems: PersonOperationalCostItemDto[];
  monthlyRetirementValue?: number | null;
  paymentType: number;
  hasDownPayment: boolean;
  downPaymentValue?: number | null;
  downPaymentDate?: string | null;
  discountFromBenefit: boolean;
  monthlyAmountForSettlement?: number | null;
  financialNotes?: string | null;
}

interface PersonFinancialEntryDto {
  id?: string | null;
  type: number;
  description?: string | null;
  value: number;
  date: string;
  origin?: string | null;
  notes?: string | null;
  createdAt: string;
}

interface PersonDocumentDto {
  id?: string | null;
  documentType: number;
  fileName?: string | null;
  description?: string | null;
  contentType?: string | null;
  createdBy?: string | null;
  uploadedAt: string;
}

interface PersonFinancialSummaryDto {
  operationalCostTotal?: number | null;
  totalPaid?: number | null;
  totalOpen?: number | null;
  outstandingBalance?: number | null;
  estimatedSalaryCountToSettle?: number | null;
  estimatedInstallmentsToSettle?: number | null;
  clientNetMonthlyValue?: number | null;
}

interface ConfirmDialogState {
  open: boolean;
  title: string;
  description: string;
  actionLabel: string;
}

const getPersonDetailsQueryKey = (personId: string) =>
  ["person-details", personId] as const;

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

const toNumberOrNull = (value: string) => {
  if (!value.trim()) return null;
  const parsed = Number(value.replace(",", "."));
  return Number.isFinite(parsed) ? parsed : null;
};

const toDateOrNull = (value: string) => (value.trim() ? value : null);

const formatCurrency = (value?: number | null) => {
  if (value === null || value === undefined || Number.isNaN(value)) return "-";
  return new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency: "BRL",
  }).format(value);
};

const paymentTypeOptions = [
  { value: "1", label: "À vista" },
  { value: "2", label: "Com entrada + saldo" },
  { value: "3", label: "Desconto integral sobre benefício" },
  { value: "4", label: "Desconto parcial sobre benefício" },
  { value: "5", label: "Personalizado" },
];

const operationalCostTypeOptions = [
  { value: "1", label: "Simples" },
  { value: "2", label: "Detalhado" },
];

const financialEntryTypeOptions = [
  { value: "1", label: "Entrada recebida" },
  { value: "2", label: "Parcela recebida" },
  { value: "3", label: "Abatimento mensal" },
  { value: "4", label: "Ajuste manual" },
  { value: "5", label: "Outro" },
];

const labelByOption = (
  options: { value: string; label: string }[],
  value?: number | null,
) => options.find((x) => x.value === String(value ?? ""))?.label ?? "-";

const extractPlainText = (value?: string | null) => {
  if (!value) return "";
  if (typeof window === "undefined") {
    return value
      .replace(/<[^>]+>/g, " ")
      .replace(/\s+/g, " ")
      .trim();
  }
  const document = new DOMParser().parseFromString(value, "text/html");
  return (document.body.textContent ?? "").replace(/\s+/g, " ").trim();
};

const sanitizeEmailHtml = (value?: string | null) => {
  if (!value) return "";
  if (typeof window === "undefined") return value;

  const document = new DOMParser().parseFromString(value, "text/html");
  document
    .querySelectorAll(
      "script, style, iframe, object, embed, link, meta, img, picture, svg, video, audio, source",
    )
    .forEach((node) => {
      node.remove();
    });

  document.querySelectorAll("*").forEach((element) => {
    Array.from(element.attributes).forEach((attribute) => {
      const attributeName = attribute.name.toLowerCase();
      const attributeValue = attribute.value.trim().toLowerCase();

      if (attributeName.startsWith("on")) {
        element.removeAttribute(attribute.name);
        return;
      }

      if (
        (attributeName === "href" || attributeName === "src") &&
        attributeValue.startsWith("javascript:")
      ) {
        element.removeAttribute(attribute.name);
      }
    });
  });

  return document.body.innerHTML;
};

const getEmailAccordionValue = (email: MonitoredEmailDto, index: number) =>
  `email-${email.id ?? email.messageUniqueId ?? email.receivedAt}-${index}`;

export default function PessoaDetalhePage() {
  const params = useParams<{ id: string }>();
  const router = useRouter();
  const searchParams = useSearchParams();
  const queryClient = useQueryClient();
  const backHref = searchParams.get("from") === "dashboard" ? "/" : "/pessoas";

  const personDetailsQuery = useQuery({
    queryKey: getPersonDetailsQueryKey(params.id),
    queryFn: async () => {
      const response = await customFetch<{ data: PersonDetailsDto }>(
        `/api/Person/${params.id}/details`,
      );
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
  const [isGovPasswordVisible, setIsGovPasswordVisible] = useState(false);
  const [govPasswordCopied, setGovPasswordCopied] = useState(false);
  const [errors, setErrors] = useState<FormErrors>({});
  const [selectedNitId, setSelectedNitId] = useState("");
  const [isSavingAgreement, setIsSavingAgreement] = useState(false);
  const [isAddingFinancialEntry, setIsAddingFinancialEntry] = useState(false);
  const [isUploadingDocument, setIsUploadingDocument] = useState(false);
  const [downloadingDocumentId, setDownloadingDocumentId] = useState<
    string | null
  >(null);
  const [viewingDocumentId, setViewingDocumentId] = useState<string | null>(
    null,
  );
  const [isDocumentDragOver, setIsDocumentDragOver] = useState(false);
  const documentFileInputRef = useRef<HTMLInputElement | null>(null);
  const [formData, setFormData] = useState({
    name: "",
    cpf: "",
    whatsApp: "",
    govPassword: "",
    birthDate: "",
  });
  const [agreementForm, setAgreementForm] = useState({
    totalCost: "",
    operationalCostType: "1",
    operationalCostSimpleValue: "",
    operationalCostItems: [
      { id: crypto.randomUUID(), description: "", value: "" },
    ],
    monthlyRetirementValue: "",
    paymentType: "5",
    hasDownPayment: false,
    downPaymentValue: "",
    downPaymentDate: "",
    discountFromBenefit: false,
    monthlyAmountForSettlement: "",
    financialNotes: "",
  });
  const [financialEntryForm, setFinancialEntryForm] = useState({
    type: "2",
    description: "",
    value: "",
    date: new Date().toISOString().slice(0, 10),
    origin: "",
    notes: "",
  });
  const [documentForm, setDocumentForm] = useState({
    file: null as File | null,
  });
  const [confirmDialog, setConfirmDialog] = useState<ConfirmDialogState>({
    open: false,
    title: "",
    description: "",
    actionLabel: "Confirmar",
  });
  const [confirmAction, setConfirmAction] = useState<
    null | (() => Promise<void>)
  >(null);
  const [isConfirmingAction, setIsConfirmingAction] = useState(false);

  const mapAgreementToForm = (
    agreement?: PersonRetirementAgreementDto | null,
  ) => ({
    totalCost: agreement?.totalCost?.toString() ?? "",
    operationalCostType: String(agreement?.operationalCostType ?? 1),
    operationalCostSimpleValue:
      agreement?.operationalCostSimpleValue?.toString() ?? "",
    operationalCostItems: agreement?.operationalCostItems?.length
      ? agreement.operationalCostItems.map((item) => ({
          id: item.id ?? crypto.randomUUID(),
          description: item.description ?? "",
          value: item.value?.toString() ?? "",
        }))
      : [{ id: crypto.randomUUID(), description: "", value: "" }],
    monthlyRetirementValue: agreement?.monthlyRetirementValue?.toString() ?? "",
    paymentType: String(agreement?.paymentType ?? 5),
    hasDownPayment: agreement?.hasDownPayment ?? false,
    downPaymentValue: agreement?.downPaymentValue?.toString() ?? "",
    downPaymentDate: toDateInputValue(agreement?.downPaymentDate),
    discountFromBenefit: agreement?.discountFromBenefit ?? false,
    monthlyAmountForSettlement:
      agreement?.monthlyAmountForSettlement?.toString() ?? "",
    financialNotes: agreement?.financialNotes ?? "",
  });

  const person = personDetailsQuery.data?.person;
  const monitoredEmails = personDetailsQuery.data?.monitoredEmails ?? [];
  const financialEntries = personDetailsQuery.data?.financialEntries ?? [];
  const documents = personDetailsQuery.data?.documents ?? [];
  const financialSummary = personDetailsQuery.data?.financialSummary;

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

  useEffect(() => {
    const agreement = personDetailsQuery.data?.retirementAgreement;
    setAgreementForm(mapAgreementToForm(agreement));
  }, [personDetailsQuery.data?.retirementAgreement]);

  const nitOptions = useMemo(() => {
    const fromApi = (
      (nitsQuery.data as getApiNitResponseSuccess | undefined)?.data.data ?? []
    ).filter((nit) => nit.id && nit.number) as { id: string; number: string }[];

    if (selectedNitId && !fromApi.some((nit) => nit.id === selectedNitId)) {
      return [
        { id: selectedNitId, number: `NIT atual (${selectedNitId})` },
        ...fromApi,
      ];
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
    setAgreementForm(
      mapAgreementToForm(personDetailsQuery.data?.retirementAgreement),
    );
    setErrors({});
    setIsEditing(false);
  };

  const savePersonChanges = async (silent = false) => {
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
        queryClient.invalidateQueries({
          queryKey: getPersonDetailsQueryKey(params.id),
        }),
      ]);

      if (!silent) {
        toast.success("Pessoa atualizada com sucesso.");
      }
      return true;
    } catch (error) {
      toast.error(
        error instanceof Error ? error.message : "Erro ao atualizar pessoa.",
      );
      return false;
    }
  };

  const handleSave = async (event: React.FormEvent) => {
    event.preventDefault();
    await handleConfirmEdit();
  };

  const openConfirmDialog = (
    state: Omit<ConfirmDialogState, "open">,
    action: () => Promise<void>,
  ) => {
    setConfirmAction(() => action);
    setConfirmDialog({
      open: true,
      ...state,
    });
  };

  const handleDialogAction = async () => {
    if (!confirmAction) return;
    setIsConfirmingAction(true);
    try {
      await confirmAction();
      setConfirmDialog((prev) => ({ ...prev, open: false }));
      setConfirmAction(null);
    } finally {
      setIsConfirmingAction(false);
    }
  };

  const handleDelete = async () => {
    openConfirmDialog(
      {
        title: "Deletar pessoa",
        description:
          "Essa ação não pode ser desfeita. Deseja realmente excluir esta pessoa?",
        actionLabel: "Deletar",
      },
      async () => {
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
      },
    );
  };

  const refreshPersonDetails = async () => {
    await queryClient.invalidateQueries({
      queryKey: getPersonDetailsQueryKey(params.id),
    });
  };

  const handleSaveAgreement = async (silent = false) => {
    setIsSavingAgreement(true);
    try {
      const operationalCostItems = agreementForm.operationalCostItems
        .map((item) => ({
          id: item.id,
          description: item.description.trim(),
          value: toNumberOrNull(item.value) ?? 0,
        }))
        .filter((item) => item.description || item.value !== 0);

      await customFetch<{ data: unknown }>(
        `/api/Person/${params.id}/retirement-agreement`,
        {
          method: "PUT",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            totalCost: toNumberOrNull(agreementForm.totalCost),
            operationalCostType: Number(agreementForm.operationalCostType),
            operationalCostSimpleValue: toNumberOrNull(
              agreementForm.operationalCostSimpleValue,
            ),
            operationalCostItems,
            monthlyRetirementValue: toNumberOrNull(
              agreementForm.monthlyRetirementValue,
            ),
            paymentType: Number(agreementForm.paymentType),
            hasDownPayment: agreementForm.hasDownPayment,
            downPaymentValue: toNumberOrNull(agreementForm.downPaymentValue),
            downPaymentDate: toDateOrNull(agreementForm.downPaymentDate),
            discountFromBenefit: agreementForm.discountFromBenefit,
            monthlyAmountForSettlement: toNumberOrNull(
              agreementForm.monthlyAmountForSettlement,
            ),
            financialNotes: agreementForm.financialNotes.trim() || null,
          }),
        },
      );

      await refreshPersonDetails();
      if (!silent) {
        toast.success("Acordo financeiro salvo com sucesso.");
      }
      return true;
    } catch (error) {
      toast.error(
        error instanceof Error
          ? error.message
          : "Erro ao salvar acordo financeiro.",
      );
      return false;
    } finally {
      setIsSavingAgreement(false);
    }
  };

  const handleConfirmEdit = async () => {
    const personSaved = await savePersonChanges(true);
    if (!personSaved) return;

    const agreementSaved = await handleSaveAgreement(true);
    if (!agreementSaved) return;

    setIsEditing(false);
    toast.success("Alterações salvas com sucesso.");
  };

  const handleAddFinancialEntry = async (event: React.FormEvent) => {
    event.preventDefault();
    const value = toNumberOrNull(financialEntryForm.value);
    if (value === null) {
      toast.error("Informe um valor válido para o lançamento.");
      return;
    }

    setIsAddingFinancialEntry(true);
    try {
      await customFetch<{ data: unknown }>(
        `/api/Person/${params.id}/financial-entries`,
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            type: Number(financialEntryForm.type),
            description: financialEntryForm.description.trim() || null,
            value,
            date: financialEntryForm.date,
            origin: financialEntryForm.origin.trim() || null,
            notes: financialEntryForm.notes.trim() || null,
          }),
        },
      );

      setFinancialEntryForm((prev) => ({
        ...prev,
        description: "",
        value: "",
        origin: "",
        notes: "",
      }));

      await refreshPersonDetails();
      toast.success("Lançamento financeiro adicionado.");
    } catch (error) {
      toast.error(
        error instanceof Error
          ? error.message
          : "Erro ao adicionar lançamento.",
      );
    } finally {
      setIsAddingFinancialEntry(false);
    }
  };

  const handleDeleteFinancialEntry = async (entryId?: string | null) => {
    if (!entryId) return;
    openConfirmDialog(
      {
        title: "Remover lançamento",
        description: "Deseja realmente remover este lançamento financeiro?",
        actionLabel: "Remover",
      },
      async () => {
        try {
          await customFetch<{ data: unknown }>(
            `/api/Person/${params.id}/financial-entries/${entryId}`,
            {
              method: "DELETE",
            },
          );
          await refreshPersonDetails();
          toast.success("Lançamento removido.");
        } catch (error) {
          toast.error(
            error instanceof Error
              ? error.message
              : "Erro ao remover lançamento.",
          );
        }
      },
    );
  };

  const handleDocumentFileSelected = (file?: File | null) => {
    setDocumentForm((prev) => ({
      ...prev,
      file: file ?? null,
    }));
  };

  const handleUploadDocument = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!documentForm.file) {
      toast.error("Selecione um arquivo para upload.");
      return;
    }

    setIsUploadingDocument(true);
    try {
      const file = documentForm.file;
      let uploaded = false;
      try {
        const createUploadUrlResponse = await customFetch<{
          data?: { storageKey?: string; uploadUrl?: string };
        }>(`/api/Person/${params.id}/documents/upload-url`, {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            fileName: file.name,
            contentType: file.type || "application/octet-stream",
          }),
        });

        const storageKey = createUploadUrlResponse.data?.storageKey;
        const uploadUrl = createUploadUrlResponse.data?.uploadUrl;
        if (!storageKey || !uploadUrl) {
          throw new Error("Não foi possível iniciar o upload do documento.");
        }

        const uploadToR2Response = await fetch(uploadUrl, {
          method: "PUT",
          headers: {
            "Content-Type": file.type || "application/octet-stream",
          },
          body: file,
        });

        if (!uploadToR2Response.ok) {
          throw new Error("Falha ao enviar arquivo para o storage.");
        }

        await customFetch<{ data: unknown }>(
          `/api/Person/${params.id}/documents/complete`,
          {
            method: "POST",
            headers: {
              "Content-Type": "application/json",
            },
            body: JSON.stringify({
              fileName: file.name,
              storageKey,
              contentType: file.type || "application/octet-stream",
            }),
          },
        );
        uploaded = true;
      } catch (directUploadError) {
        // Fallback: if direct upload is blocked (e.g. CORS), route through backend.
        const fallbackFormData = new FormData();
        fallbackFormData.append("file", file);
        await customFetch<{ data: unknown }>(`/api/Person/${params.id}/documents`, {
          method: "POST",
          body: fallbackFormData,
        });
        uploaded = true;
      }

      if (!uploaded) {
        throw new Error("Não foi possível enviar o documento.");
      }

      setDocumentForm({
        file: null,
      });
      if (documentFileInputRef.current) {
        documentFileInputRef.current.value = "";
      }

      await refreshPersonDetails();
      toast.success("Documento enviado com sucesso.");
    } catch (error) {
      toast.error(
        error instanceof Error ? error.message : "Erro ao enviar documento.",
      );
    } finally {
      setIsUploadingDocument(false);
    }
  };

  const handleDeleteDocument = async (documentId?: string | null) => {
    if (!documentId) return;
    openConfirmDialog(
      {
        title: "Remover anexo",
        description: "Deseja realmente remover este anexo?",
        actionLabel: "Remover",
      },
      async () => {
        try {
          await customFetch<{ data: unknown }>(
            `/api/Person/${params.id}/documents/${documentId}`,
            {
              method: "DELETE",
            },
          );
          await refreshPersonDetails();
          toast.success("Documento removido.");
        } catch (error) {
          toast.error(
            error instanceof Error
              ? error.message
              : "Erro ao remover documento.",
          );
        }
      },
    );
  };

  const handleDownloadDocument = async (
    documentId?: string | null,
    fileName?: string | null,
  ) => {
    if (!documentId) return;
    setDownloadingDocumentId(documentId);
    try {
      const response = await customFetch<{ data?: { url?: string } }>(
        `/api/Person/${params.id}/documents/${documentId}/download-url`,
      );
      const privateUrl = response.data?.url;
      if (!privateUrl) {
        throw new Error("Não foi possível baixar o documento.");
      }

      const anchor = document.createElement("a");
      anchor.href = privateUrl;
      anchor.download = fileName || "documento";
      anchor.rel = "noopener noreferrer";
      anchor.click();
    } catch (error) {
      toast.error(
        error instanceof Error ? error.message : "Erro ao baixar documento.",
      );
    } finally {
      setDownloadingDocumentId(null);
    }
  };

  const handleViewDocument = async (documentId?: string | null) => {
    if (!documentId) return;
    setViewingDocumentId(documentId);
    try {
      const response = await customFetch<{ data?: { url?: string } }>(
        `/api/Person/${params.id}/documents/${documentId}/view-url`,
      );
      const privateUrl = response.data?.url;
      if (!privateUrl) {
        throw new Error("Não foi possível visualizar o documento.");
      }

      window.open(privateUrl, "_blank", "noopener,noreferrer");
    } catch (error) {
      toast.error(
        error instanceof Error
          ? error.message
          : "Erro ao visualizar documento.",
      );
    } finally {
      setViewingDocumentId(null);
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
        <div className="rounded-lg border border-border bg-card p-4 sm:p-6">
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

  const personStatusStyle = getRetirementRequestStatusStyle(
    person.retirementRequestStatus,
  );
  const isSaving = updateMutation.isPending || bindPersonMutation.isPending;
  const isReadOnly = !isEditing;

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <Link
            href={backHref}
            className="mb-4 inline-flex items-center gap-1.5 text-sm text-muted-foreground transition-colors hover:text-foreground"
          >
            <ArrowLeft className="h-4 w-4" />
            Voltar
          </Link>
          <h1 className="text-xl font-semibold tracking-tight text-foreground sm:text-2xl">
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
            <span
              className={cn("h-1.5 w-1.5 rounded-full", personStatusStyle.dot)}
            />
            {getRetirementRequestStatusLabel(person.retirementRequestStatus)}
          </span>
        </div>
        <div className="flex w-full flex-wrap items-center gap-2 sm:w-auto sm:flex-nowrap">
          {!isEditing ? (
            <>
              <Button
                variant="outline"
                className="flex-1 gap-2 sm:flex-none"
                onClick={() => setIsEditing(true)}
              >
                <Pencil className="h-4 w-4" />
                Editar
              </Button>
              <Button
                className="flex-1 gap-2 sm:flex-none bg-destructive text-destructive-foreground hover:bg-destructive/90"
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
            </>
          ) : (
            <>
              <Button
                type="button"
                variant="outline"
                className="flex-1 sm:flex-none"
                onClick={handleCancelEdit}
                disabled={isSaving || isSavingAgreement}
              >
                Cancelar
              </Button>
              <Button
                type="button"
                className="flex-1 sm:flex-none"
                onClick={handleConfirmEdit}
                disabled={isSaving || isSavingAgreement}
              >
                {isSaving || isSavingAgreement ? (
                  <>
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    Confirmando...
                  </>
                ) : (
                  "Confirmar"
                )}
              </Button>
            </>
          )}
        </div>
      </div>

      <Tabs defaultValue="dados" className="gap-4">
        <div>
          <TabsList
            variant="line"
            className="w-full justify-start overflow-x-auto"
          >
            <TabsTrigger value="dados">
              <UserRound className="h-4 w-4" />
              Dados Básicos
            </TabsTrigger>
            <TabsTrigger value="financeiro">
              <HandCoins className="h-4 w-4" />
              Financeiro
            </TabsTrigger>
            <TabsTrigger value="anexos">
              <FolderOpen className="h-4 w-4" />
              Anexos
            </TabsTrigger>
          </TabsList>
        </div>

        <TabsContent value="dados" className="space-y-6">
          <section className="rounded-lg border border-border bg-card p-4 sm:p-6">
            <h2 className="text-base font-semibold text-foreground">Dados</h2>
            <div className="my-4 h-px w-full bg-border" />

            <form
              onSubmit={handleSave}
              className="space-y-5"
              autoComplete="off"
            >
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
                    <FieldLabel htmlFor="birthDate">
                      Data de Nascimento
                    </FieldLabel>
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
                    <div className="flex items-center gap-2">
                      <Input
                        id="govPassword"
                        type={isGovPasswordVisible ? "text" : "password"}
                        autoComplete="new-password"
                        value={formData.govPassword}
                        onChange={(event) =>
                          setFormData((prev) => ({
                            ...prev,
                            govPassword: event.target.value,
                          }))
                        }
                        className="min-w-0 flex-1 bg-background"
                        disabled={!isEditing || isSaving}
                      />
                      <Button
                        type="button"
                        variant="outline"
                        size="icon"
                        className="shrink-0"
                        onClick={() => setIsGovPasswordVisible((prev) => !prev)}
                        aria-label={
                          isGovPasswordVisible
                            ? "Ocultar senha"
                            : "Mostrar senha"
                        }
                        disabled={!formData.govPassword}
                      >
                        {isGovPasswordVisible ? (
                          <EyeOff className="h-4 w-4" />
                        ) : (
                          <Eye className="h-4 w-4" />
                        )}
                      </Button>
                      <Button
                        type="button"
                        variant="outline"
                        size="icon"
                        className="shrink-0"
                        onClick={async () => {
                          if (!formData.govPassword) return;
                          await navigator.clipboard.writeText(
                            formData.govPassword,
                          );
                          setGovPasswordCopied(true);
                          toast.success("Senha Gov.br copiada!");
                          setTimeout(() => setGovPasswordCopied(false), 2000);
                        }}
                        aria-label="Copiar senha"
                        disabled={!formData.govPassword}
                      >
                        {govPasswordCopied ? (
                          <Check className="h-4 w-4 text-emerald-600" />
                        ) : (
                          <Copy className="h-4 w-4" />
                        )}
                      </Button>
                    </div>
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
            </form>
          </section>

          <section className="rounded-lg border border-border bg-card p-4 sm:p-6">
            <h2 className="text-base font-semibold text-foreground">
              Histórico de e-mails monitorados
            </h2>
            <div className="my-4 h-px w-full bg-border" />

            {!monitoredEmails.length ? (
              <p className="text-sm text-muted-foreground">
                Nenhum e-mail monitorado para esta pessoa.
              </p>
            ) : (
              <Accordion
                type="single"
                collapsible
                defaultValue={getEmailAccordionValue(monitoredEmails[0], 0)}
                className="space-y-3"
              >
                {monitoredEmails.map((email, index) => {
                  const emailStatusStyle = getRetirementRequestStatusStyle(
                    email.identifiedStatus,
                  );
                  const emailSubject = extractPlainText(email.subject);
                  const renderedEmailHtml = sanitizeEmailHtml(email.rawContent);
                  const itemValue = getEmailAccordionValue(email, index);

                  return (
                    <AccordionItem
                      value={itemValue}
                      key={
                        email.id ??
                        `${email.messageUniqueId}-${email.receivedAt}`
                      }
                      className="rounded-lg border border-border bg-background px-4 last:border-b data-[state=open]:pb-4"
                    >
                      <AccordionTrigger className="hover:no-underline">
                        <div className="flex min-w-0 flex-1 flex-col items-start gap-1 overflow-hidden text-left">
                          <p className="w-full whitespace-normal break-all text-sm font-semibold text-foreground sm:break-words">
                            {emailSubject || "(sem assunto)"}
                          </p>
                          <span className="text-xs text-muted-foreground">
                            {formatDateTime(email.receivedAt)}
                          </span>
                        </div>
                      </AccordionTrigger>

                      <AccordionContent>
                        <div className="mt-2 flex flex-wrap items-center gap-3 text-xs text-muted-foreground">
                          <span>
                            <strong>Remetente:</strong> {email.from || "-"}
                          </span>
                          <span
                            className={cn(
                              "inline-flex items-center gap-2 rounded-full px-2 py-1 font-medium",
                              emailStatusStyle.bg,
                              emailStatusStyle.text,
                            )}
                          >
                            <span
                              className={cn(
                                "h-1.5 w-1.5 rounded-full",
                                emailStatusStyle.dot,
                              )}
                            />
                            {getRetirementRequestStatusLabelFromApi(
                              email.identifiedStatus,
                              email.identifiedStatusLabel,
                            )}
                          </span>
                        </div>

                        <div className="mt-3 grid gap-2 text-xs text-muted-foreground sm:grid-cols-2">
                          <p>
                            <strong>Nome extraído:</strong>{" "}
                            {email.extractedName || "-"}
                          </p>
                          <p>
                            <strong>CPF extraído:</strong>{" "}
                            {email.extractedCpf || "-"}
                          </p>
                        </div>

                        <div className="mt-3 rounded-md border border-border bg-card p-3">
                          <p className="text-xs font-medium text-foreground">
                            Resumo
                          </p>
                          <p className="mt-1 text-sm text-muted-foreground">
                            {emailSubject || "-"}
                          </p>
                        </div>

                        <div className="mt-3 rounded-md border border-border bg-card p-3">
                          <p className="text-xs font-medium text-foreground">
                            Conteúdo completo
                          </p>
                          {renderedEmailHtml ? (
                            <div
                              className="mt-1 max-h-56 overflow-auto break-words text-xs text-muted-foreground [&_*]:max-w-full [&_a]:text-foreground [&_a]:underline [&_table]:block [&_table]:w-full [&_td]:break-words [&_th]:break-words"
                              dangerouslySetInnerHTML={{
                                __html: renderedEmailHtml,
                              }}
                            />
                          ) : (
                            <p className="mt-1 text-xs text-muted-foreground">
                              -
                            </p>
                          )}
                        </div>
                      </AccordionContent>
                    </AccordionItem>
                  );
                })}
              </Accordion>
            )}
          </section>
        </TabsContent>

        <TabsContent value="financeiro">
          <section className="rounded-lg border border-border bg-card p-4 sm:p-6">
            <h2 className="text-base font-semibold text-foreground">
              Financeiro
            </h2>

            <div className="my-4 h-px w-full bg-border" />

            <div className="mt-5 space-y-5">
              <div className="mt-4 grid gap-4 sm:grid-cols-2">
                <Field>
                  <FieldLabel>Custo total acordado</FieldLabel>
                  <Input
                    value={agreementForm.totalCost}
                    onChange={(event) =>
                      setAgreementForm((prev) => ({
                        ...prev,
                        totalCost: event.target.value,
                      }))
                    }
                    placeholder="Ex: 15000"
                    type="number"
                    step="0.01"
                    disabled={isReadOnly}
                  />
                </Field>
                <Field>
                  <FieldLabel>Valor mensal da aposentadoria</FieldLabel>
                  <Input
                    value={agreementForm.monthlyRetirementValue}
                    onChange={(event) =>
                      setAgreementForm((prev) => ({
                        ...prev,
                        monthlyRetirementValue: event.target.value,
                      }))
                    }
                    placeholder="Ex: 3000"
                    type="number"
                    step="0.01"
                    disabled={isReadOnly}
                  />
                </Field>
                <Field>
                  <FieldLabel>Tipo de acordo</FieldLabel>
                  <Select
                    value={agreementForm.paymentType}
                    onValueChange={(value) =>
                      setAgreementForm((prev) => ({
                        ...prev,
                        paymentType: value,
                      }))
                    }
                    disabled={isReadOnly}
                  >
                    <SelectTrigger>
                      <SelectValue placeholder="Selecione o tipo de acordo" />
                    </SelectTrigger>
                    <SelectContent>
                      {paymentTypeOptions.map((option) => (
                        <SelectItem key={option.value} value={option.value}>
                          {option.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </Field>
                <Field>
                  <FieldLabel>Desconta do benefício?</FieldLabel>
                  <Select
                    value={agreementForm.discountFromBenefit ? "true" : "false"}
                    onValueChange={(value) =>
                      setAgreementForm((prev) => ({
                        ...prev,
                        discountFromBenefit: value === "true",
                      }))
                    }
                    disabled={isReadOnly}
                  >
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="false">Não</SelectItem>
                      <SelectItem value="true">Sim</SelectItem>
                    </SelectContent>
                  </Select>
                </Field>
                <Field>
                  <FieldLabel>Houve entrada?</FieldLabel>
                  <Select
                    value={agreementForm.hasDownPayment ? "true" : "false"}
                    onValueChange={(value) =>
                      setAgreementForm((prev) => ({
                        ...prev,
                        hasDownPayment: value === "true",
                      }))
                    }
                    disabled={isReadOnly}
                  >
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="false">Não</SelectItem>
                      <SelectItem value="true">Sim</SelectItem>
                    </SelectContent>
                  </Select>
                </Field>
                <Field>
                  <FieldLabel>Valor da entrada</FieldLabel>
                  <Input
                    value={agreementForm.downPaymentValue}
                    onChange={(event) =>
                      setAgreementForm((prev) => ({
                        ...prev,
                        downPaymentValue: event.target.value,
                      }))
                    }
                    placeholder="Ex: 2000"
                    type="number"
                    step="0.01"
                    disabled={isReadOnly}
                  />
                </Field>
                <Field>
                  <FieldLabel>Data da entrada</FieldLabel>
                  <Input
                    value={agreementForm.downPaymentDate}
                    onChange={(event) =>
                      setAgreementForm((prev) => ({
                        ...prev,
                        downPaymentDate: event.target.value,
                      }))
                    }
                    type="date"
                    disabled={isReadOnly}
                  />
                </Field>
                <Field>
                  <FieldLabel>Valor mensal para quitação</FieldLabel>
                  <Input
                    value={agreementForm.monthlyAmountForSettlement}
                    onChange={(event) =>
                      setAgreementForm((prev) => ({
                        ...prev,
                        monthlyAmountForSettlement: event.target.value,
                      }))
                    }
                    placeholder="Ex: 1000"
                    type="number"
                    step="0.01"
                    disabled={isReadOnly}
                  />
                </Field>
              </div>

              <Field className="mt-4">
                <FieldLabel>Observações financeiras</FieldLabel>
                <textarea
                  className="w-full rounded-md border border-input px-3 py-2 text-sm"
                  rows={3}
                  value={agreementForm.financialNotes}
                  onChange={(event) =>
                    setAgreementForm((prev) => ({
                      ...prev,
                      financialNotes: event.target.value,
                    }))
                  }
                  placeholder="Observações sobre o acordo, combinação com cliente, etc."
                  disabled={isReadOnly}
                />
              </Field>

              <div className="border-t py-6">
                <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                  <h3 className="text-sm font-semibold text-foreground">
                    Custo operacional
                  </h3>
                  <Select
                    value={agreementForm.operationalCostType}
                    onValueChange={(value) =>
                      setAgreementForm((prev) => ({
                        ...prev,
                        operationalCostType: value,
                      }))
                    }
                    disabled={isReadOnly}
                  >
                    <SelectTrigger className="w-full sm:w-[180px]">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {operationalCostTypeOptions.map((option) => (
                        <SelectItem key={option.value} value={option.value}>
                          {option.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>

                {agreementForm.operationalCostType === "1" ? (
                  <Field className="mt-4">
                    <FieldLabel>Custo operacional (valor único)</FieldLabel>
                    <Input
                      value={agreementForm.operationalCostSimpleValue}
                      onChange={(event) =>
                        setAgreementForm((prev) => ({
                          ...prev,
                          operationalCostSimpleValue: event.target.value,
                        }))
                      }
                      placeholder="Ex: 2500"
                      type="number"
                      step="0.01"
                      disabled={isReadOnly}
                    />
                  </Field>
                ) : (
                  <div className="mt-4 space-y-3">
                    {agreementForm.operationalCostItems.map((item, index) => (
                      <div
                        key={item.id}
                        className="grid gap-2 sm:grid-cols-[1fr_140px_auto]"
                      >
                        <Input
                          value={item.description}
                          onChange={(event) =>
                            setAgreementForm((prev) => ({
                              ...prev,
                              operationalCostItems:
                                prev.operationalCostItems.map(
                                  (current, currentIndex) =>
                                    currentIndex === index
                                      ? {
                                          ...current,
                                          description: event.target.value,
                                        }
                                      : current,
                                ),
                            }))
                          }
                          placeholder="Descrição do custo"
                          disabled={isReadOnly}
                        />
                        <Input
                          value={item.value}
                          onChange={(event) =>
                            setAgreementForm((prev) => ({
                              ...prev,
                              operationalCostItems:
                                prev.operationalCostItems.map(
                                  (current, currentIndex) =>
                                    currentIndex === index
                                      ? {
                                          ...current,
                                          value: event.target.value,
                                        }
                                      : current,
                                ),
                            }))
                          }
                          placeholder="Valor"
                          type="number"
                          step="0.01"
                          disabled={isReadOnly}
                        />
                        <Button
                          type="button"
                          variant="outline"
                          disabled={isReadOnly}
                          onClick={() =>
                            setAgreementForm((prev) => ({
                              ...prev,
                              operationalCostItems:
                                prev.operationalCostItems.length > 1
                                  ? prev.operationalCostItems.filter(
                                      (_, currentIndex) =>
                                        currentIndex !== index,
                                    )
                                  : prev.operationalCostItems,
                            }))
                          }
                        >
                          Remover
                        </Button>
                      </div>
                    ))}
                    <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
                      <Button
                        type="button"
                        variant="outline"
                        disabled={isReadOnly}
                        onClick={() =>
                          setAgreementForm((prev) => ({
                            ...prev,
                            operationalCostItems: [
                              ...prev.operationalCostItems,
                              {
                                id: crypto.randomUUID(),
                                description: "",
                                value: "",
                              },
                            ],
                          }))
                        }
                      >
                        Adicionar item
                      </Button>
                      <p className="text-sm font-medium text-foreground">
                        Total detalhado:{" "}
                        {formatCurrency(
                          agreementForm.operationalCostItems.reduce(
                            (sum, item) =>
                              sum + (toNumberOrNull(item.value) ?? 0),
                            0,
                          ),
                        )}
                      </p>
                    </div>
                  </div>
                )}
              </div>
            </div>
          </section>

          <section className="mt-6 rounded-lg border border-border bg-card p-4 sm:p-6">
            <h3 className="text-sm font-semibold text-foreground">
              Lançamentos financeiros
            </h3>

            <div className="mt-4 space-y-4">
              <form
                className="grid gap-3 sm:grid-cols-2"
                onSubmit={handleAddFinancialEntry}
              >
                <Field>
                  <FieldLabel>Tipo de lançamento</FieldLabel>
                  <Select
                    value={financialEntryForm.type}
                    onValueChange={(value) =>
                      setFinancialEntryForm((prev) => ({
                        ...prev,
                        type: value,
                      }))
                    }
                    disabled={isReadOnly}
                  >
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {financialEntryTypeOptions.map((option) => (
                        <SelectItem key={option.value} value={option.value}>
                          {option.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </Field>
                <Field>
                  <FieldLabel>Valor</FieldLabel>
                  <Input
                    value={financialEntryForm.value}
                    onChange={(event) =>
                      setFinancialEntryForm((prev) => ({
                        ...prev,
                        value: event.target.value,
                      }))
                    }
                    placeholder="Ex: 1000"
                    type="number"
                    step="0.01"
                    required
                    disabled={isReadOnly}
                  />
                </Field>
                <Field>
                  <FieldLabel>Data</FieldLabel>
                  <Input
                    value={financialEntryForm.date}
                    onChange={(event) =>
                      setFinancialEntryForm((prev) => ({
                        ...prev,
                        date: event.target.value,
                      }))
                    }
                    type="date"
                    disabled={isReadOnly}
                  />
                </Field>
                <Field>
                  <FieldLabel>Origem</FieldLabel>
                  <Input
                    value={financialEntryForm.origin}
                    onChange={(event) =>
                      setFinancialEntryForm((prev) => ({
                        ...prev,
                        origin: event.target.value,
                      }))
                    }
                    placeholder="Ex: PIX, transferência, desconto"
                    disabled={isReadOnly}
                  />
                </Field>
                <Field className="sm:col-span-2">
                  <FieldLabel>Descrição / observação</FieldLabel>
                  <Input
                    value={financialEntryForm.description}
                    onChange={(event) =>
                      setFinancialEntryForm((prev) => ({
                        ...prev,
                        description: event.target.value,
                      }))
                    }
                    placeholder="Descrição do lançamento"
                    disabled={isReadOnly}
                  />
                </Field>
                <Field className="sm:col-span-2">
                  <Input
                    value={financialEntryForm.notes}
                    onChange={(event) =>
                      setFinancialEntryForm((prev) => ({
                        ...prev,
                        notes: event.target.value,
                      }))
                    }
                    placeholder="Observações adicionais (opcional)"
                    disabled={isReadOnly}
                  />
                </Field>
                <div className="sm:col-span-2 flex justify-end">
                  <Button
                    type="submit"
                    disabled={isReadOnly || isAddingFinancialEntry}
                  >
                    {isAddingFinancialEntry
                      ? "Adicionando..."
                      : "Adicionar lançamento"}
                  </Button>
                </div>
              </form>

              {!financialEntries.length ? (
                <div className="flex h-full items-center justify-center rounded-md border border-dashed border-border p-6">
                  <p className="text-sm text-muted-foreground">
                    Nenhum lançamento financeiro registrado.
                  </p>
                </div>
              ) : (
                <div className="space-y-2">
                  {financialEntries.map((entry) => (
                    <div
                      key={entry.id}
                      className="rounded-md border border-border bg-background p-3"
                    >
                      <div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
                        <div>
                          <p className="text-sm font-medium text-foreground">
                            {labelByOption(
                              financialEntryTypeOptions,
                              entry.type,
                            )}{" "}
                            - {formatCurrency(entry.value)}
                          </p>
                          <p className="text-xs text-muted-foreground">
                            {formatDateTime(entry.date)}{" "}
                            {entry.origin ? `• ${entry.origin}` : ""}
                          </p>
                          {entry.description ? (
                            <p className="mt-1 text-xs text-muted-foreground">
                              {entry.description}
                            </p>
                          ) : null}
                          {entry.notes ? (
                            <p className="mt-1 text-xs text-muted-foreground">
                              {entry.notes}
                            </p>
                          ) : null}
                        </div>
                        <Button
                          type="button"
                          variant="outline"
                          size="sm"
                          disabled={isReadOnly}
                          onClick={() => handleDeleteFinancialEntry(entry.id)}
                        >
                          Remover
                        </Button>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </section>
        </TabsContent>

        <TabsContent value="anexos">
          <section className="rounded-lg border border-border bg-card p-4 sm:p-6">
            <h2 className="text-base font-semibold text-foreground">Anexos</h2>
            <div className="my-4 h-px w-full bg-border" />

            <form
              className="grid gap-3 sm:grid-cols-2"
              onSubmit={handleUploadDocument}
            >
              <input
                ref={documentFileInputRef}
                type="file"
                className="hidden"
                disabled={isUploadingDocument}
                onChange={(event) =>
                  handleDocumentFileSelected(event.target.files?.[0] ?? null)
                }
              />

              <div className="sm:col-span-2">
                <button
                  type="button"
                  className={cn(
                    "flex w-full flex-col items-center justify-center rounded-lg border border-dashed px-4 py-8 text-center transition-colors",
                    isUploadingDocument
                      ? "cursor-not-allowed border-border bg-muted/40 opacity-70"
                      : isDocumentDragOver
                      ? "border-foreground bg-accent"
                      : "border-border bg-background hover:border-foreground/50",
                  )}
                  disabled={isUploadingDocument}
                  onClick={() => {
                    if (isUploadingDocument) return;
                    documentFileInputRef.current?.click();
                  }}
                  onDragOver={(event) => {
                    event.preventDefault();
                    if (isUploadingDocument) return;
                    setIsDocumentDragOver(true);
                  }}
                  onDragLeave={(event) => {
                    event.preventDefault();
                    if (isUploadingDocument) return;
                    setIsDocumentDragOver(false);
                  }}
                  onDrop={(event) => {
                    event.preventDefault();
                    if (isUploadingDocument) return;
                    setIsDocumentDragOver(false);
                    handleDocumentFileSelected(
                      event.dataTransfer.files?.[0] ?? null,
                    );
                  }}
                >
                  <Upload className="h-7 w-7 text-muted-foreground" />
                  <p className="mt-3 text-sm font-medium text-foreground">
                    Arraste um arquivo aqui ou clique para selecionar
                  </p>
                  <p className="mt-1 text-xs text-muted-foreground">
                    {documentForm.file
                      ? `Arquivo selecionado: ${documentForm.file.name}`
                      : "Formatos suportados conforme seu navegador."}
                  </p>
                </button>
              </div>

              <div className="sm:col-span-2 flex justify-end">
                <Button
                  type="submit"
                  disabled={isUploadingDocument || !documentForm.file}
                >
                  {isUploadingDocument ? (
                    <>
                      <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                      Enviando...
                    </>
                  ) : (
                    "Enviar documento"
                  )}
                </Button>
              </div>
            </form>

            {!documents.length ? (
              <p className="mt-4 text-sm text-muted-foreground">
                Nenhum documento anexado.
              </p>
            ) : (
              <div className="mt-6 grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
                {documents.map((document) => (
                  <article
                    key={document.id}
                    className="rounded-lg border border-border bg-muted/20 p-4"
                  >
                    <div className="flex items-start justify-between gap-3">
                      <div className="flex min-w-0 items-start gap-3">
                        <div className="rounded-md bg-card p-2">
                          <FileText className="h-4 w-4 text-muted-foreground" />
                        </div>
                        <div className="min-w-0 flex-1">
                          <p className="truncate text-sm font-semibold text-foreground">
                            {document.fileName || "-"}
                          </p>
                          <p className="mt-0.5 text-xs text-muted-foreground">
                            {formatDateTime(document.uploadedAt)}
                          </p>
                        </div>
                      </div>

                      <div className="flex items-center gap-2">
                        <Button
                          type="button"
                          variant="outline"
                          size="icon"
                          aria-label="Ver documento"
                          title="Ver documento"
                          onClick={() => handleViewDocument(document.id)}
                          disabled={viewingDocumentId === document.id}
                        >
                          {viewingDocumentId === document.id ? (
                            <Loader2 className="h-4 w-4 animate-spin" />
                          ) : (
                            <Eye className="h-4 w-4" />
                          )}
                        </Button>
                        <Button
                          type="button"
                          variant="outline"
                          size="icon"
                          aria-label="Baixar documento"
                          title="Baixar documento"
                          onClick={() =>
                            handleDownloadDocument(document.id, document.fileName)
                          }
                          disabled={downloadingDocumentId === document.id}
                        >
                          {downloadingDocumentId === document.id ? (
                            <Loader2 className="h-4 w-4 animate-spin" />
                          ) : (
                            <Download className="h-4 w-4" />
                          )}
                        </Button>
                        <Button
                          type="button"
                          variant="outline"
                          size="icon"
                          aria-label="Remover documento"
                          title="Remover documento"
                          onClick={() => handleDeleteDocument(document.id)}
                        >
                          <Trash2 className="h-4 w-4 text-red-500" />
                        </Button>
                      </div>
                    </div>
                  </article>
                ))}
              </div>
            )}
          </section>
        </TabsContent>
      </Tabs>

      <AlertDialog
        open={confirmDialog.open}
        onOpenChange={(open) => {
          setConfirmDialog((prev) => ({ ...prev, open }));
          if (!open && !isConfirmingAction) {
            setConfirmAction(null);
          }
        }}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{confirmDialog.title}</AlertDialogTitle>
            <AlertDialogDescription>
              {confirmDialog.description}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={isConfirmingAction}>
              Cancelar
            </AlertDialogCancel>
            <AlertDialogAction
              onClick={(event) => {
                event.preventDefault();
                void handleDialogAction();
              }}
              disabled={isConfirmingAction}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              {isConfirmingAction
                ? "Processando..."
                : confirmDialog.actionLabel}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
