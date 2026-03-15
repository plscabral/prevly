"use client";

import { useState, useMemo } from "react";
import Link from "next/link";
import { Plus, Search, X, Calendar, Filter, Download } from "lucide-react";
import { PersonsTable } from "@/components/persons-table";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { mockPersons } from "@/lib/store";

export default function PessoasPage() {
  const [searchQuery, setSearchQuery] = useState("");
  const [nitFilter, setNitFilter] = useState<string>("all");
  const [ageFilter, setAgeFilter] = useState<string>("all");

  const filteredPersons = useMemo(() => {
    return mockPersons.filter((person) => {
      const matchesSearch =
        person.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        person.document.includes(searchQuery);

      const matchesNit =
        nitFilter === "all" ||
        (nitFilter === "with" && person.nitNumber) ||
        (nitFilter === "without" && !person.nitNumber);

      let matchesAge = true;
      if (ageFilter !== "all") {
        const age = person.age;
        switch (ageFilter) {
          case "under55":
            matchesAge = age < 55;
            break;
          case "55-60":
            matchesAge = age >= 55 && age <= 60;
            break;
          case "60-65":
            matchesAge = age > 60 && age <= 65;
            break;
          case "over65":
            matchesAge = age > 65;
            break;
        }
      }

      return matchesSearch && matchesNit && matchesAge;
    });
  }, [searchQuery, nitFilter, ageFilter]);

  const hasFilters = searchQuery || nitFilter !== "all" || ageFilter !== "all";

  const clearFilters = () => {
    setSearchQuery("");
    setNitFilter("all");
    setAgeFilter("all");
  };

  return (
    <div className="flex flex-col gap-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-foreground">
            Pessoas
          </h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Gerencie os clientes cadastrados no sistema
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Button variant="outline" className="gap-2">
            <Download className="h-4 w-4" />
            Extrair Relatorio
          </Button>
          <Button asChild>
            <Link href="/pessoas/nova" className="gap-2">
              <Plus className="h-4 w-4" />
              Nova Pessoa
            </Link>
          </Button>
        </div>
      </div>

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-3">
        <div className="relative flex-1 min-w-[200px] max-w-sm">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder="Buscar por nome ou CPF..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="pl-9 bg-card"
          />
        </div>

        <Select value={nitFilter} onValueChange={setNitFilter}>
          <SelectTrigger className="w-40 bg-card">
            <Filter className="mr-2 h-4 w-4 text-muted-foreground" />
            <SelectValue placeholder="NIT" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Todos</SelectItem>
            <SelectItem value="with">Com NIT</SelectItem>
            <SelectItem value="without">Sem NIT</SelectItem>
          </SelectContent>
        </Select>

        <Select value={ageFilter} onValueChange={setAgeFilter}>
          <SelectTrigger className="w-40 bg-card">
            <Calendar className="mr-2 h-4 w-4 text-muted-foreground" />
            <SelectValue placeholder="Idade" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Todas as idades</SelectItem>
            <SelectItem value="under55">Menos de 55</SelectItem>
            <SelectItem value="55-60">55 a 60 anos</SelectItem>
            <SelectItem value="60-65">60 a 65 anos</SelectItem>
            <SelectItem value="over65">Mais de 65</SelectItem>
          </SelectContent>
        </Select>

        {hasFilters && (
          <Button
            variant="ghost"
            size="sm"
            onClick={clearFilters}
            className="gap-1.5 text-muted-foreground"
          >
            <X className="h-4 w-4" />
            Limpar filtros
          </Button>
        )}
      </div>

      {/* Results count */}
      <div className="text-sm text-muted-foreground">
        {filteredPersons.length}{" "}
        {filteredPersons.length === 1
          ? "pessoa encontrada"
          : "pessoas encontradas"}
      </div>

      {/* Table */}
      <PersonsTable data={filteredPersons} />
    </div>
  );
}
