export enum NitStatus {
  PendingOwnershipCheck = 0,
  OwnershipCheckInProgress = 1,
  RejectedOwnedByAnotherPerson = 2,
  PendingContributionCalculation = 3,
  ReadyForPersonBinding = 4,
  BoundToPerson = 5,
}

export const statusLabels: Record<NitStatus, string> = {
  [NitStatus.PendingOwnershipCheck]: "Aguardando Verificação",
  [NitStatus.OwnershipCheckInProgress]: "Verificação em Andamento",
  [NitStatus.RejectedOwnedByAnotherPerson]:
    "Rejeitado - Pertence a Outra Pessoa",
  [NitStatus.PendingContributionCalculation]:
    "Calculando Contribuições",
  [NitStatus.ReadyForPersonBinding]: "Pronto para Vincular",
  [NitStatus.BoundToPerson]: "Vinculado",
};

export const statusColors: Record<
  NitStatus,
  { bg: string; text: string; dot: string }
> = {
  [NitStatus.PendingOwnershipCheck]: {
    bg: "bg-amber-50",
    text: "text-amber-700",
    dot: "bg-amber-500",
  },
  [NitStatus.OwnershipCheckInProgress]: {
    bg: "bg-blue-50",
    text: "text-blue-700",
    dot: "bg-blue-500",
  },
  [NitStatus.RejectedOwnedByAnotherPerson]: {
    bg: "bg-red-50",
    text: "text-red-700",
    dot: "bg-red-500",
  },
  [NitStatus.PendingContributionCalculation]: {
    bg: "bg-blue-50",
    text: "text-blue-700",
    dot: "bg-blue-500",
  },
  [NitStatus.ReadyForPersonBinding]: {
    bg: "bg-emerald-50",
    text: "text-emerald-700",
    dot: "bg-emerald-500",
  },
  [NitStatus.BoundToPerson]: {
    bg: "bg-emerald-50",
    text: "text-emerald-700",
    dot: "bg-emerald-500",
  },
};
