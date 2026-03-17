"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { Download, Plus, Search, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { PersonsTable } from "@/components/persons-table";
import {
  getApiPersonResponseSuccess,
  useGetApiPerson,
} from "@/lib/api/generated/person/person";

export default function PessoasPage() {
  const [searchQuery, setSearchQuery] = useState("");

  const personsQuery = useGetApiPerson({ PageNumber: 1, PageSize: 500 });
  const personsResponse = personsQuery.data as
    | getApiPersonResponseSuccess
    | undefined;
  const persons = personsResponse?.data.data ?? [];

  const filteredPersons = useMemo(() => {
    const value = searchQuery.trim().toLowerCase();
    if (!value) return persons;
    return persons.filter(
      (person) =>
        person.name?.toLowerCase().includes(value) ||
        person.cpf?.toLowerCase().includes(value),
    );
  }, [persons, searchQuery]);

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-foreground">
            Pessoas
          </h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Gerencie os clientes cadastrados.
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Button variant="outline" className="gap-2" disabled>
            <Download className="h-4 w-4" />
            Extrair Relatório
          </Button>
          <Button asChild>
            <Link href="/pessoas/nova" className="gap-2">
              <Plus className="h-4 w-4" />
              Nova Pessoa
            </Link>
          </Button>
        </div>
      </div>

      <div className="flex flex-wrap items-center gap-3">
        <div className="relative flex-1 min-w-[200px] max-w-sm">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder="Buscar por nome ou CPF..."
            value={searchQuery}
            onChange={(event) => setSearchQuery(event.target.value)}
            className="bg-card pl-9"
          />
        </div>
        {searchQuery && (
          <Button
            variant="ghost"
            size="sm"
            onClick={() => setSearchQuery("")}
            className="gap-1.5"
          >
            <X className="h-4 w-4" />
            Limpar
          </Button>
        )}
      </div>

      <div className="text-sm text-muted-foreground">
        {filteredPersons.length}{" "}
        {filteredPersons.length === 1
          ? "pessoa encontrada"
          : "pessoas encontradas"}
      </div>

      {personsQuery.isLoading ? (
        <div className="rounded-lg border border-border bg-card p-4">
          <div className="space-y-3">
            <div className="grid grid-cols-[40px_1fr_1fr_1fr_1fr_1fr_1fr_1fr] gap-3">
              <Skeleton className="h-5 w-5 rounded-sm" />
              <Skeleton className="h-4 w-24" />
              <Skeleton className="h-4 w-24" />
              <Skeleton className="h-4 w-24" />
              <Skeleton className="h-4 w-24" />
              <Skeleton className="h-4 w-24" />
              <Skeleton className="h-4 w-24" />
              <Skeleton className="h-4 w-24" />
            </div>
            {Array.from({ length: 5 }).map((_, index) => (
              <div
                key={`persons-grid-skeleton-${index}`}
                className="grid grid-cols-[40px_1fr_1fr_1fr_1fr_1fr_1fr_1fr] items-center gap-3"
              >
                <Skeleton className="h-5 w-5 rounded-sm" />
                <Skeleton className="h-5 w-32" />
                <Skeleton className="h-5 w-32" />
                <Skeleton className="h-5 w-32" />
                <Skeleton className="h-5 w-32" />
                <Skeleton className="h-5 w-32" />
                <Skeleton className="h-5 w-32" />
                <Skeleton className="h-5 w-32" />
              </div>
            ))}
          </div>
        </div>
      ) : personsQuery.isError ? (
        <div className="rounded-lg border border-red-200 bg-red-50 p-8 text-sm text-red-700">
          Erro ao carregar pessoas.
        </div>
      ) : (
        <PersonsTable data={filteredPersons} />
      )}
    </div>
  );
}
