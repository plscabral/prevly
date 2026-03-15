'use client'

import { useState, useMemo } from 'react'
import { Plus, Search, X, Calendar, Filter, Download } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'
import { NitsTable } from '@/components/nits-table'
import { ImportNitsDialog } from '@/components/import-nits-dialog'
import { mockNITs } from '@/lib/store'
import { SocialSecurityRegistrationStatus, statusLabels } from '@/lib/types'

export default function NitsPage() {
  const [searchQuery, setSearchQuery] = useState('')
  const [statusFilter, setStatusFilter] = useState<string>('all')
  const [yearFilter, setYearFilter] = useState<string>('all')
  const [importOpen, setImportOpen] = useState(false)

  const filteredNITs = useMemo(() => {
    return mockNITs.filter((nit) => {
      const matchesSearch = nit.number.toLowerCase().includes(searchQuery.toLowerCase())
      const matchesStatus = statusFilter === 'all' || nit.status.toString() === statusFilter
      const matchesYear = yearFilter === 'all' || 
        new Date(nit.createdAt).getFullYear().toString() === yearFilter

      return matchesSearch && matchesStatus && matchesYear
    })
  }, [searchQuery, statusFilter, yearFilter])

  const years = useMemo(() => {
    const uniqueYears = [...new Set(mockNITs.map(nit => new Date(nit.createdAt).getFullYear()))]
    return uniqueYears.sort((a, b) => b - a)
  }, [])

  const hasFilters = searchQuery || statusFilter !== 'all' || yearFilter !== 'all'

  const clearFilters = () => {
    setSearchQuery('')
    setStatusFilter('all')
    setYearFilter('all')
  }

  return (
    <div className="flex flex-col gap-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-foreground">NITs</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Gerencie os registros de Número de Identificação do Trabalhador
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Button variant="outline" className="gap-2">
            <Download className="h-4 w-4" />
            Extrair Relatorio
          </Button>
          <Dialog open={importOpen} onOpenChange={setImportOpen}>
            <DialogTrigger asChild>
              <Button className="gap-2">
                <Plus className="h-4 w-4" />
                Importar NITs
              </Button>
            </DialogTrigger>
          <DialogContent className="max-w-2xl">
            <DialogHeader>
              <DialogTitle>Importar NITs de PDF</DialogTitle>
            </DialogHeader>
            <ImportNitsDialog onClose={() => setImportOpen(false)} />
            </DialogContent>
          </Dialog>
        </div>
      </div>

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-3">
        <div className="relative flex-1 min-w-[200px] max-w-sm">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder="Buscar por número..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="pl-9 bg-card"
          />
        </div>

        <Select value={statusFilter} onValueChange={setStatusFilter}>
          <SelectTrigger className="w-[200px] bg-card">
            <Filter className="mr-2 h-4 w-4 text-muted-foreground" />
            <SelectValue placeholder="Status" />
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

        <Select value={yearFilter} onValueChange={setYearFilter}>
          <SelectTrigger className="w-[140px] bg-card">
            <Calendar className="mr-2 h-4 w-4 text-muted-foreground" />
            <SelectValue placeholder="Ano" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Todos os Anos</SelectItem>
            {years.map((year) => (
              <SelectItem key={year} value={year.toString()}>
                {year}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        {hasFilters && (
          <Button variant="ghost" size="sm" onClick={clearFilters} className="gap-1.5 text-muted-foreground">
            <X className="h-4 w-4" />
            Limpar filtros
          </Button>
        )}
      </div>

      {/* Results count */}
      <div className="text-sm text-muted-foreground">
        {filteredNITs.length} {filteredNITs.length === 1 ? 'registro encontrado' : 'registros encontrados'}
      </div>

      {/* Table */}
      <NitsTable data={filteredNITs} />
    </div>
  )
}
