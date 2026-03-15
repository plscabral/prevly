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
import { Eye, EyeOff, Copy, Check, ArrowUpDown, MoreHorizontal, Pencil, Trash2 } from 'lucide-react'
import { Person } from '@/lib/types'
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
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { toast } from 'sonner'

interface PersonsTableProps {
  data: Person[]
}

function PasswordCell({ password }: { password: string }) {
  const [isVisible, setIsVisible] = useState(false)
  const [copied, setCopied] = useState(false)

  const handleCopy = async () => {
    await navigator.clipboard.writeText(password)
    setCopied(true)
    toast.success('Senha copiada!')
    setTimeout(() => setCopied(false), 2000)
  }

  return (
    <div className="flex items-center gap-1.5">
      <code className="rounded bg-muted px-1.5 py-0.5 font-mono text-xs">
        {isVisible ? password : '••••••••'}
      </code>
      <Button
        variant="ghost"
        size="sm"
        onClick={() => setIsVisible(!isVisible)}
        className="h-7 w-7 p-0"
      >
        {isVisible ? (
          <EyeOff className="h-3.5 w-3.5 text-muted-foreground" />
        ) : (
          <Eye className="h-3.5 w-3.5 text-muted-foreground" />
        )}
      </Button>
      <Button
        variant="ghost"
        size="sm"
        onClick={handleCopy}
        className="h-7 w-7 p-0"
      >
        {copied ? (
          <Check className="h-3.5 w-3.5 text-emerald-600" />
        ) : (
          <Copy className="h-3.5 w-3.5 text-muted-foreground" />
        )}
      </Button>
    </div>
  )
}

const columns: ColumnDef<Person>[] = [
  {
    accessorKey: 'name',
    header: ({ column }) => (
      <Button
        variant="ghost"
        size="sm"
        onClick={() => column.toggleSorting(column.getIsSorted() === 'asc')}
        className="-ml-3 h-8 font-medium"
      >
        Nome
        <ArrowUpDown className="ml-1.5 h-3.5 w-3.5" />
      </Button>
    ),
    cell: ({ row }) => (
      <span className="font-medium">{row.getValue('name')}</span>
    ),
  },
  {
    accessorKey: 'document',
    header: 'CPF',
    cell: ({ row }) => (
      <span className="font-mono text-sm text-muted-foreground">{row.getValue('document')}</span>
    ),
  },
  {
    accessorKey: 'age',
    header: ({ column }) => (
      <Button
        variant="ghost"
        size="sm"
        onClick={() => column.toggleSorting(column.getIsSorted() === 'asc')}
        className="-ml-3 h-8 font-medium"
      >
        Idade
        <ArrowUpDown className="ml-1.5 h-3.5 w-3.5" />
      </Button>
    ),
    cell: ({ row }) => (
      <span className="tabular-nums">{row.getValue('age')} anos</span>
    ),
  },
  {
    accessorKey: 'govPassword',
    header: 'Senha Gov.br',
    cell: ({ row }) => <PasswordCell password={row.getValue('govPassword')} />,
  },
  {
    accessorKey: 'nitNumber',
    header: 'NIT Vinculado',
    cell: ({ row }) => {
      const nit = row.getValue('nitNumber') as string | undefined
      return nit ? (
        <span className="font-mono text-sm">{nit}</span>
      ) : (
        <span className="text-muted-foreground">-</span>
      )
    },
  },
  {
    accessorKey: 'createdAt',
    header: 'Cadastrado em',
    cell: ({ row }) => {
      const date = row.getValue('createdAt') as Date
      return (
        <span className="text-sm text-muted-foreground">
          {new Date(date).toLocaleDateString('pt-BR')}
        </span>
      )
    },
  },
  {
    id: 'actions',
    cell: ({ row }) => {
      const person = row.original
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
                navigator.clipboard.writeText(person.govPassword)
                toast.success('Senha copiada')
              }}
            >
              <Copy className="mr-2 h-4 w-4" />
              Copiar Senha
            </DropdownMenuItem>
            <DropdownMenuItem onClick={() => toast.info('Funcionalidade em desenvolvimento')}>
              <Pencil className="mr-2 h-4 w-4" />
              Editar
            </DropdownMenuItem>
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

export function PersonsTable({ data }: PersonsTableProps) {
  const [sorting, setSorting] = useState<SortingState>([])

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
                Nenhuma pessoa encontrada.
              </TableCell>
            </TableRow>
          )}
        </TableBody>
      </Table>
    </div>
  )
}
