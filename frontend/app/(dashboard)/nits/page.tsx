"use client";

import { useMemo, useState } from "react";
import { FileDown, Filter, Plus, RefreshCw, Search, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { ImportNitsDialog } from "@/components/import-nits-dialog";
import { NitsTable } from "@/components/nits-table";
import { statusLabels } from "@/lib/types";
import {
  getApiNitResponseSuccess,
  getGetApiNitQueryKey,
  useGetApiNit,
  usePostApiNitBindPerson,
} from "@/lib/api/generated/nit/nit";
import {
  getApiPersonResponseSuccess,
  getGetApiPersonQueryKey,
  useGetApiPerson,
} from "@/lib/api/generated/person/person";
import { Nit } from "@/lib/api/generated/model";
import { toast } from "sonner";
import { useQueryClient } from "@tanstack/react-query";
import { customFetchRaw } from "@/lib/api/http-client";

export default function NitsPage() {
  const queryClient = useQueryClient();
  const [searchQuery, setSearchQuery] = useState("");
  const [statusFilter, setStatusFilter] = useState<string>("all");
  const [importOpen, setImportOpen] = useState(false);
  const [forceRefreshing, setForceRefreshing] = useState(false);
  const [selectedNits, setSelectedNits] = useState<
    Nit[]
  >([]);
  const [isExporting, setIsExporting] = useState(false);

  const nitsQuery = useGetApiNit({
    PageNumber: 1,
    PageSize: 500,
  });
  const personsQuery = useGetApiPerson({ PageNumber: 1, PageSize: 500 });
  const bindPersonMutation = usePostApiNitBindPerson();

  const nits =
    (
      nitsQuery.data as
        | getApiNitResponseSuccess
        | undefined
    )?.data.data ?? [];
  const persons =
    (personsQuery.data as getApiPersonResponseSuccess | undefined)?.data.data ??
    [];

  const personNamesById = useMemo(
    () =>
      Object.fromEntries(
        persons
          .filter((person) => person.id && person.name)
          .map((person) => [person.id!, person.name!]),
      ),
    [persons],
  );

  const filteredNits = useMemo(
    () =>
      nits.filter((nit) => {
        const query = searchQuery.toLowerCase();
        const matchesSearch =
          (nit.number ?? "").toLowerCase().includes(query) ||
          (nit.ownershipOwnerName ?? "").toLowerCase().includes(query);
        const matchesStatus =
          statusFilter === "all" || `${nit.status}` === statusFilter;
        return matchesSearch && matchesStatus;
      }),
    [nits, searchQuery, statusFilter],
  );

  const onBindPerson = async (nitId?: string | null) => {
    if (!nitId) return;
    const personId = window.prompt(
      "Informe o ID da pessoa para vincular este NIT:",
    );
    if (!personId) return;

    try {
      await bindPersonMutation.mutateAsync({
        data: { nitId: nitId, personId },
      });
      await queryClient.invalidateQueries({
        queryKey: getGetApiNitQueryKey(),
      });
      toast.success("NIT vinculado com sucesso.");
    } catch (error) {
      toast.error(
        error instanceof Error ? error.message : "Erro ao vincular NIT.",
      );
    }
  };

  const hasFilters = searchQuery || statusFilter !== "all";
  const isRefreshing =
    forceRefreshing || nitsQuery.isRefetching || personsQuery.isRefetching;

  const handleRefresh = async () => {
    setForceRefreshing(true);
    try {
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: getGetApiNitQueryKey(),
        }),
        queryClient.invalidateQueries({ queryKey: getGetApiPersonQueryKey() }),
      ]);
      await Promise.all([nitsQuery.refetch(), personsQuery.refetch()]);
    } finally {
      setForceRefreshing(false);
    }
  };

  const handleExport = async () => {
    const selectedIds = selectedNits
      .map((nit) => nit.id)
      .filter((id): id is string => Boolean(id));

    if (!selectedIds.length && !filteredNits.length) {
      toast.error("Nenhum NIT para exportar.");
      return;
    }

    setIsExporting(true);
    try {
      const response = await customFetchRaw(
        "/api/Nit/export",
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            query: searchQuery.trim() || null,
            status: statusFilter === "all" ? null : Number(statusFilter),
            nitIds: selectedIds,
          }),
        },
      );

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(errorText || "Erro ao exportar relatorio de NITs.");
      }

      const blob = await response.blob();
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement("a");
      anchor.href = url;
      anchor.download = `prevly-nits-${new Date().toISOString().slice(0, 10)}.xlsx`;
      anchor.click();
      URL.revokeObjectURL(url);

      toast.success(
        selectedIds.length
          ? `${selectedIds.length} NIT(s) exportado(s).`
          : "Relatório exportado com filtros aplicados.",
      );
    } catch (error) {
      toast.error(
        error instanceof Error
          ? error.message
          : "Erro ao exportar relatorio de NITs.",
      );
    } finally {
      setIsExporting(false);
    }
  };

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-foreground">
            NITs
          </h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Gerencie os registros de NIT.
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Dialog open={importOpen} onOpenChange={setImportOpen}>
            <DialogTrigger asChild>
              <Button className="w-full gap-2 sm:w-auto">
                <Plus className="h-4 w-4" />
                Importar NITs
              </Button>
            </DialogTrigger>
            <DialogContent className="max-w-2xl">
              <DialogHeader className="mb-3">
                <DialogTitle>Importar NITs</DialogTitle>
              </DialogHeader>
              <ImportNitsDialog onClose={() => setImportOpen(false)} />
            </DialogContent>
          </Dialog>
        </div>
      </div>

      <div className="flex flex-wrap items-center gap-3">
        <div className="relative w-full sm:flex-1 sm:min-w-[200px] sm:max-w-sm">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder="Buscar por número..."
            value={searchQuery}
            onChange={(event) => setSearchQuery(event.target.value)}
            className="bg-card pl-9"
          />
        </div>

        <div className="w-full sm:w-auto">
          <Select value={statusFilter} onValueChange={setStatusFilter}>
            <SelectTrigger className="w-full bg-card sm:w-[220px]">
              <span className="flex items-center gap-2">
                <Filter className="h-4 w-4 text-muted-foreground" />
                <SelectValue placeholder="Status" />
              </span>
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Todos os Status</SelectItem>
              {Object.entries(statusLabels).map(([value, label]) => (
                <SelectItem key={value} value={value}>
                  {label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        {hasFilters && (
          <Button
            variant="ghost"
            size="sm"
            onClick={() => {
              setSearchQuery("");
              setStatusFilter("all");
            }}
            className="gap-1.5 text-muted-foreground"
          >
            <X className="h-4 w-4" />
            Limpar filtros
          </Button>
        )}
      </div>

      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div className="text-sm text-muted-foreground">
          {filteredNits.length}{" "}
          {filteredNits.length === 1
            ? "registro encontrado"
            : "registros encontrados"}
        </div>
        <div className="flex w-full flex-wrap items-center gap-2 sm:w-auto sm:flex-nowrap">
          <Button
            variant="outline"
            size="sm"
            className="flex-1 gap-2 sm:flex-none"
            onClick={handleRefresh}
            disabled={isRefreshing}
          >
            <RefreshCw
              className={isRefreshing ? "h-4 w-4 animate-spin" : "h-4 w-4"}
            />
            Atualizar
          </Button>
          <Button
            variant="outline"
            size="sm"
            className="flex-1 gap-2 sm:flex-none"
            onClick={handleExport}
            disabled={isExporting}
          >
            <FileDown className="h-4 w-4" />
            Extrair Relatório
          </Button>
        </div>
      </div>

      {nitsQuery.isLoading || forceRefreshing ? (
        <div className="rounded-lg border border-border bg-card p-4">
          <div className="space-y-3 md:hidden">
            {Array.from({ length: 5 }).map((_, index) => (
              <div key={`nits-mobile-skeleton-${index}`} className="space-y-2 rounded-md border border-border p-3">
                <Skeleton className="h-4 w-2/3" />
                <Skeleton className="h-4 w-1/2" />
              </div>
            ))}
          </div>
          <div className="hidden space-y-3 md:block">
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
                key={`nits-grid-skeleton-${index}`}
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
      ) : nitsQuery.isError ? (
        <div className="rounded-lg border border-red-200 bg-red-50 p-8 text-sm text-red-700">
          Erro ao carregar NITs.
        </div>
      ) : (
        <NitsTable
          data={filteredNits}
          personNamesById={personNamesById}
          onBindPerson={(nit) => onBindPerson(nit.id)}
          onSelectionChange={setSelectedNits}
        />
      )}
    </div>
  );
}
