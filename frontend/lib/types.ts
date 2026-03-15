export interface Person {
  id: string
  name: string
  document: string // CPF
  age: number
  govPassword: string
  nitNumber?: string
  createdAt: Date
}

export enum SocialSecurityRegistrationStatus {
  PendingOwnershipCheck = 0,
  OwnershipCheckInProgress = 1,
  RejectedOwnedByAnotherPerson = 2,
  PendingContributionCalculation = 3,
  ReadyForPersonBinding = 4,
  BoundToPerson = 5,
}

export interface NIT {
  id: string
  number: string
  firstContributionDate: Date
  lastContributionDate: Date
  contributionYears: number
  createdAt: Date
  personId?: string
  status: SocialSecurityRegistrationStatus
  ownershipCheckedAt?: Date
  lastProcessingError?: string
}

export const statusLabels: Record<SocialSecurityRegistrationStatus, string> = {
  [SocialSecurityRegistrationStatus.PendingOwnershipCheck]: 'Aguardando Verificação',
  [SocialSecurityRegistrationStatus.OwnershipCheckInProgress]: 'Verificação em Andamento',
  [SocialSecurityRegistrationStatus.RejectedOwnedByAnotherPerson]: 'Rejeitado - Pertence a Outra Pessoa',
  [SocialSecurityRegistrationStatus.PendingContributionCalculation]: 'Calculando Contribuições',
  [SocialSecurityRegistrationStatus.ReadyForPersonBinding]: 'Pronto para Vincular',
  [SocialSecurityRegistrationStatus.BoundToPerson]: 'Vinculado',
}

export const statusColors: Record<SocialSecurityRegistrationStatus, { bg: string; text: string; dot: string }> = {
  [SocialSecurityRegistrationStatus.PendingOwnershipCheck]: { 
    bg: 'bg-amber-50', text: 'text-amber-700', dot: 'bg-amber-500' 
  },
  [SocialSecurityRegistrationStatus.OwnershipCheckInProgress]: { 
    bg: 'bg-blue-50', text: 'text-blue-700', dot: 'bg-blue-500' 
  },
  [SocialSecurityRegistrationStatus.RejectedOwnedByAnotherPerson]: { 
    bg: 'bg-red-50', text: 'text-red-700', dot: 'bg-red-500' 
  },
  [SocialSecurityRegistrationStatus.PendingContributionCalculation]: { 
    bg: 'bg-blue-50', text: 'text-blue-700', dot: 'bg-blue-500' 
  },
  [SocialSecurityRegistrationStatus.ReadyForPersonBinding]: { 
    bg: 'bg-emerald-50', text: 'text-emerald-700', dot: 'bg-emerald-500' 
  },
  [SocialSecurityRegistrationStatus.BoundToPerson]: { 
    bg: 'bg-emerald-50', text: 'text-emerald-700', dot: 'bg-emerald-500' 
  },
}
