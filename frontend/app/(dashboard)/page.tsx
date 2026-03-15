'use client'

import Link from 'next/link'
import { Users, FileText, AlertCircle, CheckCircle, Clock, ArrowRight } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { mockPersons, mockNITs } from '@/lib/store'
import { SocialSecurityRegistrationStatus } from '@/lib/types'

export default function DashboardPage() {
  const totalPersons = mockPersons.length
  const personsWithNit = mockPersons.filter(p => p.nitNumber).length
  
  const totalNITs = mockNITs.length
  const boundNITs = mockNITs.filter(n => n.status === SocialSecurityRegistrationStatus.BoundToPerson).length
  const pendingNITs = mockNITs.filter(n => 
    n.status === SocialSecurityRegistrationStatus.PendingOwnershipCheck ||
    n.status === SocialSecurityRegistrationStatus.PendingContributionCalculation ||
    n.status === SocialSecurityRegistrationStatus.ReadyForPersonBinding
  ).length
  const processingNITs = mockNITs.filter(n => 
    n.status === SocialSecurityRegistrationStatus.OwnershipCheckInProgress
  ).length

  const stats = [
    {
      title: 'Total de Pessoas',
      value: totalPersons,
      description: `${personsWithNit} com NIT vinculado`,
      icon: Users,
      href: '/pessoas',
      color: 'text-foreground',
    },
    {
      title: 'NITs Vinculados',
      value: boundNITs,
      description: `de ${totalNITs} registros`,
      icon: CheckCircle,
      href: '/nits',
      color: 'text-emerald-600',
    },
    {
      title: 'Pendentes',
      value: pendingNITs,
      description: 'aguardando acao',
      icon: AlertCircle,
      href: '/nits',
      color: 'text-amber-600',
    },
    {
      title: 'Em Processamento',
      value: processingNITs,
      description: 'verificacao em andamento',
      icon: Clock,
      href: '/nits',
      color: 'text-blue-600',
    },
  ]

  const recentPersons = mockPersons.slice(0, 5)
  const recentNITs = mockNITs.slice(0, 5)

  return (
    <div className="flex flex-col gap-8">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-semibold tracking-tight text-foreground">Dashboard</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Visao geral do sistema de gestao de aposentadorias
        </p>
      </div>

      {/* Stats Grid */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {stats.map((stat) => (
          <Card key={stat.title} className="relative overflow-hidden">
            <CardHeader className="flex flex-row items-center justify-between pb-2">
              <CardTitle className="text-sm font-medium text-muted-foreground">
                {stat.title}
              </CardTitle>
              <stat.icon className={`h-4 w-4 ${stat.color}`} />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold text-foreground">{stat.value}</div>
              <p className="text-xs text-muted-foreground mt-1">{stat.description}</p>
              <Link 
                href={stat.href}
                className="absolute inset-0"
                aria-label={`Ver ${stat.title.toLowerCase()}`}
              />
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Quick Actions & Recent Activity */}
      <div className="grid gap-6 lg:grid-cols-2">
        {/* Recent Persons */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between">
            <CardTitle className="text-base font-semibold">Pessoas Recentes</CardTitle>
            <Button variant="ghost" size="sm" asChild>
              <Link href="/pessoas" className="gap-1.5">
                Ver todas
                <ArrowRight className="h-4 w-4" />
              </Link>
            </Button>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {recentPersons.map((person) => (
                <div key={person.id} className="flex items-center justify-between py-2 border-b border-border last:border-0">
                  <div>
                    <p className="text-sm font-medium text-foreground">{person.name}</p>
                    <p className="text-xs text-muted-foreground">{person.document}</p>
                  </div>
                  <div className="text-right">
                    <p className="text-sm text-muted-foreground">{person.age} anos</p>
                    {person.nitNumber && (
                      <p className="text-xs text-emerald-600">NIT vinculado</p>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>

        {/* Recent NITs */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between">
            <CardTitle className="text-base font-semibold">NITs Recentes</CardTitle>
            <Button variant="ghost" size="sm" asChild>
              <Link href="/nits" className="gap-1.5">
                Ver todos
                <ArrowRight className="h-4 w-4" />
              </Link>
            </Button>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {recentNITs.map((nit) => (
                <div key={nit.id} className="flex items-center justify-between py-2 border-b border-border last:border-0">
                  <div>
                    <p className="text-sm font-medium font-mono text-foreground">{nit.number}</p>
                    <p className="text-xs text-muted-foreground">
                      {nit.contributionYears} anos de contribuicao
                    </p>
                  </div>
                  <div className="text-right">
                    <p className="text-xs text-muted-foreground">
                      {new Date(nit.createdAt).toLocaleDateString('pt-BR')}
                    </p>
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
