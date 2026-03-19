export enum RetirementRequestStatus {
  PendingRequirement = 0,
  Approved = 1,
  Denied = 2,
  UnderAnalysis = 3,
}

export const retirementRequestStatusLabels: Record<RetirementRequestStatus, string> = {
  [RetirementRequestStatus.PendingRequirement]: "Aguardando exigência(s)",
  [RetirementRequestStatus.Approved]: "Benefício aprovado",
  [RetirementRequestStatus.Denied]: "Benefício negado",
  [RetirementRequestStatus.UnderAnalysis]: "Em análise",
};

export const retirementRequestStatusStyles: Record<
  RetirementRequestStatus,
  { bg: string; text: string; dot: string }
> = {
  [RetirementRequestStatus.PendingRequirement]: {
    bg: "bg-amber-50",
    text: "text-amber-700",
    dot: "bg-amber-500",
  },
  [RetirementRequestStatus.Approved]: {
    bg: "bg-emerald-50",
    text: "text-emerald-700",
    dot: "bg-emerald-500",
  },
  [RetirementRequestStatus.Denied]: {
    bg: "bg-rose-50",
    text: "text-rose-700",
    dot: "bg-rose-500",
  },
  [RetirementRequestStatus.UnderAnalysis]: {
    bg: "bg-sky-50",
    text: "text-sky-700",
    dot: "bg-sky-500",
  },
};

export function getRetirementRequestStatusLabel(status?: number | null): string {
  if (status === null || status === undefined) return "Sem status";
  return retirementRequestStatusLabels[status as RetirementRequestStatus] ?? "Sem status";
}

export function getRetirementRequestStatusLabelFromApi(
  status?: number | null,
  apiLabel?: string | null,
): string {
  const normalizedApiLabel = apiLabel?.trim();
  if (!normalizedApiLabel) return getRetirementRequestStatusLabel(status);

  const loweredApiLabel = normalizedApiLabel.toLowerCase();

  if (loweredApiLabel === "deferido") {
    return "Benefício aprovado";
  }

  if (loweredApiLabel === "indeferido") {
    return "Benefício negado";
  }

  if (
    loweredApiLabel === "aguardando cumprimento de exigência" ||
    loweredApiLabel === "aguardando cumprimento de exigencia"
  ) {
    return "Aguardando exigência(s)";
  }

  return normalizedApiLabel;
}

export function getRetirementRequestStatusStyle(status?: number | null) {
  if (status === null || status === undefined) {
    return {
      bg: "bg-zinc-100",
      text: "text-zinc-700",
      dot: "bg-zinc-500",
    };
  }

  return retirementRequestStatusStyles[status as RetirementRequestStatus] ?? {
    bg: "bg-zinc-100",
    text: "text-zinc-700",
    dot: "bg-zinc-500",
  };
}
