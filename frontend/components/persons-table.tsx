"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { formatDistanceToNow } from "date-fns";
import { ptBR } from "date-fns/locale";
import {
  ColumnDef,
  flexRender,
  getCoreRowModel,
  getPaginationRowModel,
  getSortedRowModel,
  PaginationState,
  RowSelectionState,
  SortingState,
  useReactTable,
} from "@tanstack/react-table";
import { ArrowUpDown, CalendarClock, FileText } from "lucide-react";
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
import {
  getRetirementRequestStatusLabel,
  getRetirementRequestStatusStyle,
} from "@/lib/person-retirement-status";
import { cn } from "@/lib/utils";

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

export function PersonsTable({ data, onSelectionChange }: PersonsTableProps) {
  const PAGE_SIZE = 25;
  const [sorting, setSorting] = useState<SortingState>([]);
  const [rowSelection, setRowSelection] = useState<RowSelectionState>({});
  const [pagination, setPagination] = useState<PaginationState>({
    pageIndex: 0,
    pageSize: PAGE_SIZE,
  });

  const formatCpfDisplay = (value?: string | null) => {
    if (!value) return "-";
    const digits = value.replace(/\D/g, "").slice(0, 11);
    if (!digits) return "-";
    return digits
      .replace(/^(\d{3})(\d)/, "$1.$2")
      .replace(/^(\d{3})\.(\d{3})(\d)/, "$1.$2.$3")
      .replace(/\.(\d{3})(\d)/, ".$1-$2");
  };

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
        <span className="text-sm text-muted-foreground">{formatCpfDisplay(row.original.cpf)}</span>
      ),
    },
    {
      id: "retirementRequestStatus",
      header: "Status monitorado",
      cell: ({ row }) => {
        const status = row.original.retirementRequestStatus;
        const style = getRetirementRequestStatusStyle(status);
        return (
          <span
            className={cn(
              "inline-flex items-center gap-2 rounded-full px-2 py-1 text-xs font-medium",
              style.bg,
              style.text,
            )}
          >
            <span className={cn("h-1.5 w-1.5 rounded-full", style.dot)} />
            {getRetirementRequestStatusLabel(status)}
          </span>
        );
      },
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
    getPaginationRowModel: getPaginationRowModel(),
    onSortingChange: setSorting,
    onPaginationChange: setPagination,
    onRowSelectionChange: setRowSelection,
    enableRowSelection: true,
    state: { sorting, rowSelection, pagination },
  });

  useEffect(() => {
    if (!onSelectionChange) return;
    const selected = table.getSelectedRowModel().rows.map((row) => row.original);
    onSelectionChange(selected);
  }, [onSelectionChange, table, rowSelection]);

  useEffect(() => {
    table.setPageIndex(0);
  }, [data, table]);

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
      <div className="flex items-center justify-between border-t border-border px-4 py-3 text-sm text-muted-foreground">
        <span>
          Página {table.getState().pagination.pageIndex + 1} de {Math.max(1, table.getPageCount())}
        </span>
        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => table.previousPage()}
            disabled={!table.getCanPreviousPage()}
          >
            Anterior
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={() => table.nextPage()}
            disabled={!table.getCanNextPage()}
          >
            Próxima
          </Button>
        </div>
      </div>
    </div>
  );
}
