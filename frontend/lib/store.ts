import { Person, NIT, SocialSecurityRegistrationStatus } from './types'

// Mock data store (em produção, isso seria um banco de dados)
export const mockPersons: Person[] = [
  {
    id: '1',
    name: 'João Silva',
    document: '123.456.789-00',
    age: 58,
    govPassword: 'senha123!',
    nitNumber: '12345678901',
    createdAt: new Date('2024-01-15'),
  },
  {
    id: '2',
    name: 'Maria Santos',
    document: '987.654.321-00',
    age: 62,
    govPassword: 'maria@2024',
    nitNumber: '98765432100',
    createdAt: new Date('2024-02-20'),
  },
  {
    id: '3',
    name: 'Carlos Oliveira',
    document: '456.789.123-00',
    age: 55,
    govPassword: 'C@rlos2024',
    createdAt: new Date('2024-03-10'),
  },
]

export const mockNITs: NIT[] = [
  {
    id: '1',
    number: '12345678901',
    firstContributionDate: new Date('1990-03-15'),
    lastContributionDate: new Date('2024-01-15'),
    contributionYears: 34,
    createdAt: new Date('2024-01-15'),
    personId: '1',
    status: SocialSecurityRegistrationStatus.BoundToPerson,
    ownershipCheckedAt: new Date('2024-01-16'),
  },
  {
    id: '2',
    number: '98765432100',
    firstContributionDate: new Date('1985-06-20'),
    lastContributionDate: new Date('2024-02-28'),
    contributionYears: 39,
    createdAt: new Date('2024-02-20'),
    personId: '2',
    status: SocialSecurityRegistrationStatus.BoundToPerson,
    ownershipCheckedAt: new Date('2024-02-21'),
  },
  {
    id: '3',
    number: '11122233344',
    firstContributionDate: new Date('2000-01-10'),
    lastContributionDate: new Date('2023-12-31'),
    contributionYears: 24,
    createdAt: new Date('2024-03-01'),
    status: SocialSecurityRegistrationStatus.ReadyForPersonBinding,
    ownershipCheckedAt: new Date('2024-03-02'),
  },
  {
    id: '4',
    number: '55566677788',
    firstContributionDate: new Date('1995-08-05'),
    lastContributionDate: new Date('2024-03-01'),
    contributionYears: 29,
    createdAt: new Date('2024-03-05'),
    status: SocialSecurityRegistrationStatus.PendingOwnershipCheck,
  },
]

export const availableNITs = mockNITs.filter(
  (nit) => nit.status === SocialSecurityRegistrationStatus.ReadyForPersonBinding ||
           nit.status === SocialSecurityRegistrationStatus.PendingOwnershipCheck
)
