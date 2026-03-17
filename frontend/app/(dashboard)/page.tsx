"use client";

import Link from "next/link";
import { AlertCircle, ArrowRight, CheckCircle, Clock, FileText, Users } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import {
  getApiPersonResponseSuccess,
  useGetApiPerson,
} from "@/lib/api/generated/person/person";
import {
  getApiSocialSecurityRegistrationResponseSuccess,
  useGetApiSocialSecurityRegistration,
} from "@/lib/api/generated/social-security-registration/social-security-registration";
import { SocialSecurityRegistrationStatus } from "@/lib/api/generated/model";

export default function DashboardPage() {
  const personsQuery = useGetApiPerson({ PageNumber: 1, PageSize: 500 });
  const nitsQuery = useGetApiSocialSecurityRegistration({ PageNumber: 1, PageSize: 500 });

  const persons = (personsQuery.data as getApiPersonResponseSuccess | undefined)?.data.data ?? [];
  const nits =
    (nitsQuery.data as getApiSocialSecurityRegistrationResponseSuccess | undefined)?.data.data ?? [];

  const totalPersons = persons.length;
  const personsWithNit = persons.filter((person) => person.socialSecurityRegistrationId).length;
  const totalNits = nits.length;
  const boundNits = nits.filter((nit) => nit.status === SocialSecurityRegistrationStatus.NUMBER_5).length;
  const pendingNits = nits.filter(
    (nit) =>
      nit.status === SocialSecurityRegistrationStatus.NUMBER_0 ||
      nit.status === SocialSecurityRegistrationStatus.NUMBER_3 ||
      nit.status === SocialSecurityRegistrationStatus.NUMBER_4,
  ).length;
  const processingNits = nits.filter(
    (nit) => nit.status === SocialSecurityRegistrationStatus.NUMBER_1,
  ).length;

  const stats = [
    {
      title: "Total de Pessoas",
      value: totalPersons,
      description: `${personsWithNit} com NIT vinculado`,
      icon: Users,
      href: "/pessoas",
      color: "text-foreground",
    },
    {
      title: "NITs Vinculados",
      value: boundNits,
      description: `de ${totalNits} registros`,
      icon: CheckCircle,
      href: "/nits",
      color: "text-emerald-600",
    },
    {
      title: "Pendentes",
      value: pendingNits,
      description: "aguardando ação",
      icon: AlertCircle,
      href: "/nits",
      color: "text-amber-600",
    },
    {
      title: "Em Processamento",
      value: processingNits,
      description: "verificação em andamento",
      icon: Clock,
      href: "/nits",
      color: "text-blue-600",
    },
  ];

  const recentPersons = persons.slice(0, 5);
  const recentNits = nits.slice(0, 5);
  const dashboardLoading = personsQuery.isLoading || nitsQuery.isLoading;

  return (
    <div className="flex flex-col gap-8">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight text-foreground">Dashboard</h1>
        <p className="mt-1 text-sm text-muted-foreground">Visão geral do sistema.</p>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {dashboardLoading
          ? Array.from({ length: 4 }).map((_, index) => (
              <Card key={`stats-skeleton-${index}`} className="overflow-hidden">
                <CardHeader className="pb-2">
                  <Skeleton className="h-4 w-28" />
                </CardHeader>
                <CardContent>
                  <Skeleton className="h-8 w-16" />
                  <Skeleton className="mt-2 h-3 w-24" />
                </CardContent>
              </Card>
            ))
          : stats.map((stat) => (
              <Card key={stat.title} className="relative overflow-hidden">
                <CardHeader className="flex flex-row items-center justify-between pb-2">
                  <CardTitle className="text-sm font-medium text-muted-foreground">{stat.title}</CardTitle>
                  <stat.icon className={`h-4 w-4 ${stat.color}`} />
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-bold text-foreground">{stat.value}</div>
                  <p className="mt-1 text-xs text-muted-foreground">{stat.description}</p>
                  <Link href={stat.href} className="absolute inset-0" aria-label={`Ver ${stat.title.toLowerCase()}`} />
                </CardContent>
              </Card>
            ))}
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
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
            {dashboardLoading ? (
              <div className="space-y-3">
                <Skeleton className="h-12 w-full" />
                <Skeleton className="h-12 w-full" />
                <Skeleton className="h-12 w-full" />
                <Skeleton className="h-12 w-full" />
                <Skeleton className="h-12 w-full" />
              </div>
            ) : recentPersons.length ? (
              <div className="space-y-3">
                {recentPersons.map((person) => (
                  <div key={person.id} className="flex items-center justify-between border-b border-border py-2 last:border-0">
                    <div>
                      <p className="text-sm font-medium text-foreground">{person.name}</p>
                      <p className="text-xs text-muted-foreground">{person.cpf}</p>
                    </div>
                    <div className="text-right">
                      <p className="text-sm text-muted-foreground">{person.age ? `${person.age} anos` : "-"}</p>
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <div className="flex min-h-[220px] flex-col items-center justify-center gap-2 text-center text-muted-foreground">
                <FileText className="h-7 w-7 text-muted-foreground" />
                <p className="text-sm text-muted-foreground">Nenhum registro encontrado.</p>
              </div>
            )}
          </CardContent>
        </Card>

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
            {dashboardLoading ? (
              <div className="space-y-3">
                <Skeleton className="h-12 w-full" />
                <Skeleton className="h-12 w-full" />
                <Skeleton className="h-12 w-full" />
                <Skeleton className="h-12 w-full" />
                <Skeleton className="h-12 w-full" />
              </div>
            ) : recentNits.length ? (
              <div className="space-y-3">
                {recentNits.map((nit) => (
                  <div key={nit.id} className="flex items-center justify-between border-b border-border py-2 last:border-0">
                    <div>
                      <p className="text-sm font-medium text-foreground">{nit.number}</p>
                      <p className="text-xs text-muted-foreground">
                        {nit.contributionYears ? `${nit.contributionYears} anos de contribuição` : "-"}
                      </p>
                    </div>
                    <div className="text-right">
                      <p className="text-xs text-muted-foreground">
                        {nit.createdAt ? new Date(nit.createdAt).toLocaleDateString("pt-BR") : "-"}
                      </p>
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <div className="flex min-h-[220px] flex-col items-center justify-center gap-2 text-center text-muted-foreground">
                <FileText className="h-7 w-7 text-muted-foreground" />
                <p className="text-sm text-muted-foreground">Nenhum registro encontrado.</p>
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
