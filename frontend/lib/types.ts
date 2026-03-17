export enum NitStatus {
  PendingVerification = 0,
  VerificationInProgress = 1,
  NotFound = 2,
  Unbound = 3,
  Bound = 4,
  PendingPeriodExtraction = 5,
  PeriodExtractionInProgress = 6,
  ReadyToUse = 7,
  QueryError = 8,
}

export const statusLabels: Record<NitStatus, string> = {
  [NitStatus.PendingVerification]: "Aguardando verificação",
  [NitStatus.VerificationInProgress]: "Verificação em andamento",
  [NitStatus.NotFound]: "Não encontrado",
  [NitStatus.Unbound]: "Não vinculado",
  [NitStatus.Bound]: "Vinculado",
  [NitStatus.PendingPeriodExtraction]: "Aguardando extração de período",
  [NitStatus.PeriodExtractionInProgress]: "Extração em andamento",
  [NitStatus.ReadyToUse]: "Pronto para uso",
  [NitStatus.QueryError]: "Erro na consulta",
};

export const statusColors: Record<
  NitStatus,
  { bg: string; text: string; dot: string }
> = {
  [NitStatus.PendingVerification]: {
    bg: "bg-amber-50",
    text: "text-amber-700",
    dot: "bg-amber-500",
  },
  [NitStatus.VerificationInProgress]: {
    bg: "bg-blue-50",
    text: "text-blue-700",
    dot: "bg-blue-500",
  },
  [NitStatus.NotFound]: {
    bg: "bg-red-50",
    text: "text-red-700",
    dot: "bg-red-500",
  },
  [NitStatus.Unbound]: {
    bg: "bg-zinc-100",
    text: "text-zinc-700",
    dot: "bg-zinc-500",
  },
  [NitStatus.Bound]: {
    bg: "bg-emerald-50",
    text: "text-emerald-700",
    dot: "bg-emerald-500",
  },
  [NitStatus.PendingPeriodExtraction]: {
    bg: "bg-amber-50",
    text: "text-amber-700",
    dot: "bg-amber-500",
  },
  [NitStatus.PeriodExtractionInProgress]: {
    bg: "bg-blue-50",
    text: "text-blue-700",
    dot: "bg-blue-500",
  },
  [NitStatus.ReadyToUse]: {
    bg: "bg-emerald-50",
    text: "text-emerald-700",
    dot: "bg-emerald-500",
  },
  [NitStatus.QueryError]: {
    bg: "bg-rose-50",
    text: "text-rose-700",
    dot: "bg-rose-500",
  },
};
