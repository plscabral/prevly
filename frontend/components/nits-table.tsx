'use client'

import { useState } from 'react'
import {
  ColumnDef,
  flexRender,
  getCoreRowModel,
  useReactTable,
  getSortedRowModel,
  SortingState,
} from '@tanstack/react-table'
import { ArrowUpDown, AlertCircle, MoreHorizontal, Link2, UserPlus, Trash2 } from 'lucide-react'
import { NIT, statusLabels, statusColors, SocialSecurityRegistrationStatus } from '@/lib/types'
import { Button } from '@/components/ui/button'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@/components/ui/tooltip'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { cn } from '@/lib/utils'
import { toast } from 'sonner'
import { mockPersons } from '@/lib/store'

interface NitsTableProps {
  data: NIT[]
}

function StatusBadge({ status }: { status: SocialSecurityRegistrationStatus }) {
  const colors = statusColors[status]
  return (
    <span className={cn(
      'inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-medium',
      colors.bg, colors.text
    )}>
      <span className={cn('h-1.5 w-1.5 rounded-full', colors.dot)} />
      {statusLabels[status]}
    </span>
  )
}

function ErrorTooltip({ error }: { error?: string }) {
  if (!error) return null
  
  return (
    <TooltipProvider>
      <Tooltip>
        <TooltipTrigger asChild>
          <AlertCircle className="h-4 w-4 text-red-500" />
        </TooltipTrigger>
        <TooltipContent>
          <p className="max-w-xs">{error}</p>
        </TooltipContent>
      </Tooltip>
    </TooltipProvider>
  )
}

export function NitsTable({ data }: NitsTableProps) {
  const [sorting, setSorting] = useState<SortingState>([])

  const columns: ColumnDef<NIT>[] = [
    {
      accessorKey: 'number',
      header: ({ column }) => (
        <Button
          variant="ghost"
          size="sm"
          onClick={() => column.toggleSorting(column.getIsSorted() === 'asc')}
          className="-ml-3 h-8 font-medium"
        >
          Número NIT
          <ArrowUpDown className="ml-1.5 h-3.5 w-3.5" />
        </Button>
      ),
      cell: ({ row }) => (
        <span className="font-mono text-sm">{row.getValue('number')}</span>
      ),
    },
    {
      accessorKey: 'personId',
      header: 'Pessoa Vinculada',
      cell: ({ row }) => {
        const personId = row.getValue('personId') as string | undefined
        const person = personId ? mockPersons.find(p => p.id === personId) : null
        
        if (!person) {
          return <span className="text-muted-foreground">-</span>
        }
        
        return (
          <span className="text-sm">{person.name}</span>
        )
      },
    },
    {
      accessorKey: 'contributionYears',
      header: ({ column }) => (
        <Button
          variant="ghost"
          size="sm"
          onClick={() => column.toggleSorting(column.getIsSorted() === 'asc')}
          className="-ml-3 h-8 font-medium"
        >
          Anos de Contribuição
          <ArrowUpDown className="ml-1.5 h-3.5 w-3.5" />
        </Button>
      ),
      cell: ({ row }) => (
        <span className="tabular-nums">{row.getValue('contributionYears')} anos</span>
      ),
    },
    {
      accessorKey: 'firstContributionDate',
      header: 'Primeira Contribuição',
      cell: ({ row }) => {
        const date = row.getValue('firstContributionDate') as Date
        return (
          <span className="text-sm text-muted-foreground">
            {new Date(date).toLocaleDateString('pt-BR')}
          </span>
        )
      },
    },
    {
      accessorKey: 'lastContributionDate',
      header: 'Última Contribuição',
      cell: ({ row }) => {
        const date = row.getValue('lastContributionDate') as Date
        return (
          <span className="text-sm text-muted-foreground">
            {new Date(date).toLocaleDateString('pt-BR')}
          </span>
        )
      },
    },
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => <StatusBadge status={row.getValue('status')} />,
    },
    {
      accessorKey: 'lastProcessingError',
      header: '',
      cell: ({ row }) => <ErrorTooltip error={row.getValue('lastProcessingError')} />,
      size: 40,
    },
    {
      id: 'actions',
      cell: ({ row }) => {
        const nit = row.original
        return (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="sm" className="h-8 w-8 p-0">
                <MoreHorizontal className="h-4 w-4" />
                <span className="sr-only">Abrir menu</span>
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem 
                onClick={() => {
                  navigator.clipboard.writeText(nit.number)
                  toast.success('NIT copiado')
                }}
              >
                <Link2 className="mr-2 h-4 w-4" />
                Copiar NIT
              </DropdownMenuItem>
              {nit.status === SocialSecurityRegistrationStatus.ReadyForPersonBinding && (
                <DropdownMenuItem onClick={() => toast.info('Funcionalidade em desenvolvimento')}>
                  <UserPlus className="mr-2 h-4 w-4" />
                  Vincular a Pessoa
                </DropdownMenuItem>
              )}
              <DropdownMenuSeparator />
              <DropdownMenuItem 
                className="text-red-600 focus:text-red-600"
                onClick={() => toast.info('Funcionalidade em desenvolvimento')}
              >
                <Trash2 className="mr-2 h-4 w-4" />
                Remover
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        )
      },
      size: 50,
    },
  ]

  const table = useReactTable({
    data,
    columns,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
    onSortingChange: setSorting,
    state: {
      sorting,
    },
  })

  return (
    <div className="rounded-lg border border-border bg-card overflow-hidden">
      <Table>
        <TableHeader>
          {table.getHeaderGroups().map((headerGroup) => (
            <TableRow key={headerGroup.id} className="hover:bg-transparent">
              {headerGroup.headers.map((header) => (
                <TableHead key={header.id} className="h-11 text-xs font-medium text-muted-foreground">
                  {header.isPlaceholder
                    ? null
                    : flexRender(
                        header.column.columnDef.header,
                        header.getContext()
                      )}
                </TableHead>
              ))}
            </TableRow>
          ))}
        </TableHeader>
        <TableBody>
          {table.getRowModel().rows?.length ? (
            table.getRowModel().rows.map((row) => (
              <TableRow key={row.id} className="group">
                {row.getVisibleCells().map((cell) => (
                  <TableCell key={cell.id} className="py-3">
                    {flexRender(
                      cell.column.columnDef.cell,
                      cell.getContext()
                    )}
                  </TableCell>
                ))}
              </TableRow>
            ))
          ) : (
            <TableRow>
              <TableCell
                colSpan={columns.length}
                className="h-32 text-center text-muted-foreground"
              >
                Nenhum NIT encontrado.
              </TableCell>
            </TableRow>
          )}
        </TableBody>
      </Table>
    </div>
  )
}
