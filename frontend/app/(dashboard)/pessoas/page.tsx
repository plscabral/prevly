"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { FileDown, Plus, RefreshCw, Search, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { PersonsTable } from "@/components/persons-table";
import {
  getApiPersonResponseSuccess,
  getGetApiPersonQueryKey,
  useGetApiPerson,
} from "@/lib/api/generated/person/person";
import { Person } from "@/lib/api/generated/model";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { customFetchRaw } from "@/lib/api/http-client";

export default function PessoasPage() {
  const queryClient = useQueryClient();
  const [searchQuery, setSearchQuery] = useState("");
  const [forceRefreshing, setForceRefreshing] = useState(false);
  const [selectedPersons, setSelectedPersons] = useState<Person[]>([]);
  const [isExporting, setIsExporting] = useState(false);

  const personsQuery = useGetApiPerson({ PageNumber: 1, PageSize: 500 });
  const personsResponse = personsQuery.data as
    | getApiPersonResponseSuccess
    | undefined;
  const persons = personsResponse?.data.data ?? [];

  const filteredPersons = useMemo(() => {
    const value = searchQuery.trim().toLowerCase();
    const queryDigits = value.replace(/\D/g, "");
    if (!value) return persons;
    return persons.filter(
      (person) => {
        const personCpf = person.cpf ?? "";
        const personCpfDigits = personCpf.replace(/\D/g, "");
        const matchesName = person.name?.toLowerCase().includes(value);
        const matchesCpfText = personCpf.toLowerCase().includes(value);
        const matchesCpfDigits = queryDigits ? personCpfDigits.includes(queryDigits) : false;
        return matchesName || matchesCpfText || matchesCpfDigits;
      },
    );
  }, [persons, searchQuery]);

  const isRefreshing = forceRefreshing || personsQuery.isRefetching;

  const handleRefresh = async () => {
    setForceRefreshing(true);
    try {
      await queryClient.invalidateQueries({ queryKey: getGetApiPersonQueryKey() });
      await personsQuery.refetch();
    } finally {
      setForceRefreshing(false);
    }
  };

  const handleExport = async () => {
    const selectedIds = selectedPersons
      .map((person) => person.id)
      .filter((id): id is string => Boolean(id));

    if (!selectedIds.length && !filteredPersons.length) {
      toast.error("Nenhuma pessoa para exportar.");
      return;
    }

    setIsExporting(true);
    try {
      const response = await customFetchRaw("/api/Person/export", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          query: searchQuery.trim() || null,
          personIds: selectedIds,
        }),
      });

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(errorText || "Erro ao exportar relatorio de pessoas.");
      }

      const blob = await response.blob();
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement("a");
      anchor.href = url;
      anchor.download = `prevly-pessoas-${new Date().toISOString().slice(0, 10)}.xlsx`;
      anchor.click();
      URL.revokeObjectURL(url);

      toast.success(selectedIds.length
        ? `${selectedIds.length} pessoa(s) exportada(s).`
        : "Relatório exportado com filtros aplicados.");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Erro ao exportar relatorio de pessoas.");
    } finally {
      setIsExporting(false);
    }
  };

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

      <div className="flex items-center justify-between gap-3">
        <div className="text-sm text-muted-foreground">
          {filteredPersons.length}{" "}
          {filteredPersons.length === 1
            ? "pessoa encontrada"
            : "pessoas encontradas"}
        </div>
        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            className="gap-2"
            onClick={handleRefresh}
            disabled={isRefreshing}
          >
            <RefreshCw className={isRefreshing ? "h-4 w-4 animate-spin" : "h-4 w-4"} />
            Atualizar
          </Button>
          <Button
            variant="outline"
            size="sm"
            className="gap-2"
            onClick={handleExport}
            disabled={isExporting}
          >
            <FileDown className="h-4 w-4" />
            Extrair Relatório
          </Button>
        </div>
      </div>

      {personsQuery.isLoading || forceRefreshing ? (
        <div className="rounded-lg border border-border bg-card p-4">
          <div className="space-y-3">
            <div className="grid grid-cols-[40px_40px_1.4fr_1fr_1.5fr_1.3fr_1fr_1fr_0.9fr] gap-3">
              <Skeleton className="h-5 w-5 rounded-sm" />
              <Skeleton className="h-5 w-5 rounded-sm" />
              <Skeleton className="h-4 w-24" />
              <Skeleton className="h-4 w-16" />
              <Skeleton className="h-4 w-28" />
              <Skeleton className="h-4 w-20" />
              <Skeleton className="h-4 w-16" />
              <Skeleton className="h-4 w-20" />
              <Skeleton className="h-4 w-20" />
            </div>
            {Array.from({ length: 5 }).map((_, index) => (
              <div
                key={`persons-grid-skeleton-${index}`}
                className="grid grid-cols-[40px_40px_1.4fr_1fr_1.5fr_1.3fr_1fr_1fr_0.9fr] items-center gap-3"
              >
                <Skeleton className="h-5 w-5 rounded-sm" />
                <Skeleton className="h-8 w-8 rounded-md" />
                <Skeleton className="h-5 w-[85%]" />
                <Skeleton className="h-5 w-[75%]" />
                <Skeleton className="h-5 w-[70%]" />
                <Skeleton className="h-5 w-[90%]" />
                <Skeleton className="h-5 w-[80%]" />
                <Skeleton className="h-5 w-[80%]" />
                <Skeleton className="h-5 w-[70%]" />
              </div>
            ))}
          </div>
        </div>
      ) : personsQuery.isError ? (
        <div className="rounded-lg border border-red-200 bg-red-50 p-8 text-sm text-red-700">
          Erro ao carregar pessoas.
        </div>
      ) : (
        <PersonsTable data={filteredPersons} onSelectionChange={setSelectedPersons} />
      )}
    </div>
  );
}
