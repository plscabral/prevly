"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { formatDistanceToNow } from "date-fns";
import { ptBR } from "date-fns/locale";
import {
  ColumnDef,
  flexRender,
  getCoreRowModel,
  getSortedRowModel,
  RowSelectionState,
  SortingState,
  useReactTable,
} from "@tanstack/react-table";
import { ArrowUpDown, CalendarClock, Check, Copy, Eye, EyeOff, FileText } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Person } from "@/lib/api/generated/model";
import { toast } from "sonner";

interface PersonsTableProps {
  data: Person[];
  onSelectionChange?: (selected: Person[]) => void;
}

function formatDistanceFromNowPtBr(value?: string | null) {
  if (!value) return null;
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return null;
  return formatDistanceToNow(date, { addSuffix: true, locale: ptBR });
}

function PasswordCell({ password }: { password?: string | null }) {
  const [isVisible, setIsVisible] = useState(false);
  const [copied, setCopied] = useState(false);
  const safePassword = password ?? "";

  if (!safePassword) return <span className="text-muted-foreground">-</span>;

  const handleCopy = async () => {
    await navigator.clipboard.writeText(safePassword);
    setCopied(true);
    toast.success("Senha copiada!");
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <div className="flex items-center gap-1.5">
      <code className="rounded bg-muted px-1.5 py-0.5 text-xs">
        {isVisible ? safePassword : "••••••••"}
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
      <Button variant="ghost" size="sm" onClick={handleCopy} className="h-7 w-7 p-0">
        {copied ? (
          <Check className="h-3.5 w-3.5 text-emerald-600" />
        ) : (
          <Copy className="h-3.5 w-3.5 text-muted-foreground" />
        )}
      </Button>
    </div>
  );
}

export function PersonsTable({ data, onSelectionChange }: PersonsTableProps) {
  const [sorting, setSorting] = useState<SortingState>([]);
  const [rowSelection, setRowSelection] = useState<RowSelectionState>({});

  const columns: ColumnDef<Person>[] = [
    {
      id: "select",
      header: ({ table }) => (
        <div className="pl-2">
          <Checkbox
            checked={
              table.getIsAllPageRowsSelected() ||
              (table.getIsSomePageRowsSelected() && "indeterminate")
            }
            onCheckedChange={(value) => table.toggleAllPageRowsSelected(!!value)}
            aria-label="Selecionar todos"
          />
        </div>
      ),
      cell: ({ row }) => (
        <div className="pl-2">
          <Checkbox
            checked={row.getIsSelected()}
            onCheckedChange={(value) => row.toggleSelected(!!value)}
            aria-label="Selecionar linha"
          />
        </div>
      ),
      enableSorting: false,
      enableHiding: false,
      size: 52,
    },
    {
      id: "details",
      header: "",
      cell: ({ row }) => (
        <Button asChild variant="ghost" size="sm" className="h-8 w-8 p-0">
          <Link href={`/pessoas/${row.original.id}`}>
            <FileText className="h-4 w-4" />
            <span className="sr-only">Abrir detalhe</span>
          </Link>
        </Button>
      ),
      enableSorting: false,
      enableHiding: false,
      size: 40,
    },
    {
      accessorKey: "name",
      header: ({ column }) => (
        <Button
          variant="ghost"
          size="sm"
          onClick={() => column.toggleSorting(column.getIsSorted() === "asc")}
          className="-ml-3 h-8 text-xs font-medium"
        >
          Nome
          <ArrowUpDown className="ml-1.5 size-3" />
        </Button>
      ),
      cell: ({ row }) => <span className="font-medium">{row.original.name ?? "-"}</span>,
    },
    {
      accessorKey: "cpf",
      header: "CPF",
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground">{row.original.cpf ?? "-"}</span>
      ),
    },
    {
      accessorKey: "govPassword",
      header: "Senha Gov.br",
      cell: ({ row }) => <PasswordCell password={row.original.govPassword} />,
    },
    {
      id: "mainContact",
      header: "WhatsApp",
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground">
          {row.original.phone || row.original.whatsApp || "-"}
        </span>
      ),
    },
    {
      id: "nitNumber",
      header: "NIT Vinculado",
      cell: ({ row }) => (
        <span className="text-sm">
          {row.original.nitId || "-"}
        </span>
      ),
    },
    {
      accessorKey: "createdAt",
      header: "Criado em",
      cell: ({ row }) => {
        const createdAtDistance = formatDistanceFromNowPtBr(row.original.createdAt);
        if (!createdAtDistance) return <span className="text-sm text-muted-foreground">-</span>;

        return (
          <p className="flex items-center text-xs text-muted-foreground">
            <CalendarClock className="mr-2 h-3.5 w-3.5" />
            {createdAtDistance}
          </p>
        );
      },
    },
  ];

  const table = useReactTable({
    data,
    columns,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
    onSortingChange: setSorting,
    onRowSelectionChange: setRowSelection,
    enableRowSelection: true,
    state: { sorting, rowSelection },
  });

  useEffect(() => {
    if (!onSelectionChange) return;
    const selected = Object.entries(rowSelection)
      .filter(([, isSelected]) => isSelected)
      .map(([rowId]) => data[Number(rowId)])
      .filter((person): person is Person => Boolean(person));
    onSelectionChange(selected);
  }, [onSelectionChange, rowSelection, data]);

  return (
    <div className="overflow-hidden rounded-lg border border-border bg-card">
      <Table>
        <TableHeader>
          {table.getHeaderGroups().map((headerGroup) => (
            <TableRow key={headerGroup.id} className="hover:bg-transparent">
              {headerGroup.headers.map((header) => (
                <TableHead key={header.id} className="h-11 text-xs font-medium text-muted-foreground">
                  {header.isPlaceholder
                    ? null
                    : flexRender(header.column.columnDef.header, header.getContext())}
                </TableHead>
              ))}
            </TableRow>
          ))}
        </TableHeader>
        <TableBody>
          {table.getRowModel().rows.length ? (
            table.getRowModel().rows.map((row) => (
              <TableRow key={row.id} className="group">
                {row.getVisibleCells().map((cell) => (
                  <TableCell key={cell.id} className="py-3">
                    {flexRender(cell.column.columnDef.cell, cell.getContext())}
                  </TableCell>
                ))}
              </TableRow>
            ))
          ) : (
            <TableRow>
              <TableCell colSpan={columns.length} className="h-32 text-center text-muted-foreground">
                Nenhuma pessoa encontrada.
              </TableCell>
            </TableRow>
          )}
        </TableBody>
      </Table>
    </div>
  );
}
