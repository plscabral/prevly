"use client";

import { useEffect, useState } from "react";
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
import {
  AlertCircle,
  ArrowUpDown,
  CalendarClock,
  CalendarDays,
  Link2,
  MoreHorizontal,
  Timer,
  UserPlus,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import { cn } from "@/lib/utils";
import {
  Nit,
  NitStatus,
} from "@/lib/api/generated/model";
import { statusColors, statusLabels } from "@/lib/types";
import { toast } from "sonner";

interface NitsTableProps {
  data: Nit[];
  personNamesById: Record<string, string>;
  onBindPerson: (nit: Nit) => void;
  onSelectionChange?: (selected: Nit[]) => void;
}

function StatusBadge({ status }: { status?: number }) {
  if (status === undefined || status === null) {
    return <span className="text-muted-foreground">-</span>;
  }

  const typedStatus = status as keyof typeof statusLabels;
  const colors = statusColors[typedStatus];
  const label = statusLabels[typedStatus];

  if (!colors || !label) {
    return <span className="text-muted-foreground">-</span>;
  }

  return (
    <span
      className={cn(
        "inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-medium",
        colors.bg,
        colors.text,
      )}
    >
      <span className={cn("h-1.5 w-1.5 rounded-full", colors.dot)} />
      {label}
    </span>
  );
}

function ErrorTooltip({ error }: { error?: string | null }) {
  if (!error) return null;

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
  );
}

function formatPtBrDate(value?: string | null) {
  if (!value) return null;
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return null;
  return date.toLocaleDateString("pt-BR");
}

function formatDistanceFromNowPtBr(value?: string | null) {
  if (!value) return null;
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return null;
  return formatDistanceToNow(date, { addSuffix: true, locale: ptBR });
}

export function NitsTable({
  data,
  personNamesById,
  onBindPerson,
  onSelectionChange,
}: NitsTableProps) {
  const [sorting, setSorting] = useState<SortingState>([]);
  const [rowSelection, setRowSelection] = useState<RowSelectionState>({});

  const columns: ColumnDef<Nit>[] = [
    {
      id: "select",
      header: ({ table }) => (
        <div className="pl-2">
          <Checkbox
            checked={
              table.getIsAllPageRowsSelected() ||
              (table.getIsSomePageRowsSelected() && "indeterminate")
            }
            onCheckedChange={(value) =>
              table.toggleAllPageRowsSelected(!!value)
            }
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
      accessorKey: "number",
      header: ({ column }) => (
        <Button
          variant="ghost"
          size="sm"
          onClick={() => column.toggleSorting(column.getIsSorted() === "asc")}
          className="-ml-3 h-8 text-xs font-medium"
        >
          Número NIT
          <ArrowUpDown className="ml-1.5 size-3" />
        </Button>
      ),
      cell: ({ row }) => (
        <span className="text-sm">{row.original.number ?? "-"}</span>
      ),
    },
    {
      accessorKey: "status",
      header: "Status",
      cell: ({ row }) => <StatusBadge status={row.original.status} />,
    },
    {
      accessorKey: "personId",
      header: "Pessoa",
      cell: ({ row }) => {
        const personId = row.original.personId;
        if (!personId) return <span className="text-muted-foreground">-</span>;
        return (
          <span className="text-sm">
            {personNamesById[personId] ?? personId}
          </span>
        );
      },
    },
    {
      accessorKey: "ownershipOwnerName",
      header: "Titular",
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground">
          {row.original.ownershipOwnerName ?? "-"}
        </span>
      ),
    },
    {
      id: "contributionSummary",
      header: "Período de contribuição",
      cell: ({ row }) => {
        const years = row.original.contributionYears;
        const firstDate = formatPtBrDate(row.original.firstContributionDate);
        const lastDate = formatPtBrDate(row.original.lastContributionDate);
        const hasData =
          (years !== null && years !== undefined) ||
          firstDate !== null ||
          lastDate !== null;

        if (!hasData) {
          return <span className="text-sm text-muted-foreground">-</span>;
        }

        return (
          <p className="flex items-center text-xs text-muted-foreground">
            <CalendarDays className="h-3.5 w-3.5 mr-2" />
            {firstDate} - {lastDate}
          </p>
        );
      },
    },
    {
      accessorKey: "createdAt",
      header: "Criado em",
      cell: ({ row }) => {
        const createdAtDistance = formatDistanceFromNowPtBr(row.original.createdAt);

        if (!createdAtDistance) {
          return <span className="text-sm text-muted-foreground">-</span>;
        }

        return (
          <p className="flex items-center text-xs text-muted-foreground">
            <CalendarClock className="mr-2 h-3.5 w-3.5" />
            {createdAtDistance}
          </p>
        );
      },
    },
    {
      accessorKey: "lastProcessingError",
      header: "",
      cell: ({ row }) => (
        <ErrorTooltip error={row.original.lastProcessingError} />
      ),
      size: 40,
    },
    {
      id: "actions",
      cell: ({ row }) => {
        const nit = row.original;
        return (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="sm" className="h-8 w-8 p-0">
                <MoreHorizontal className="h-4 w-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem
                onClick={() => {
                  if (nit.number) {
                    navigator.clipboard.writeText(nit.number);
                    toast.success("NIT copiado");
                  }
                }}
              >
                <Link2 className="mr-2 h-4 w-4" />
                Copiar NIT
              </DropdownMenuItem>
              {nit.status ===
                NitStatus.NUMBER_4 && (
                <DropdownMenuItem onClick={() => onBindPerson(nit)}>
                  <UserPlus className="mr-2 h-4 w-4" />
                  Vincular a Pessoa
                </DropdownMenuItem>
              )}
            </DropdownMenuContent>
          </DropdownMenu>
        );
      },
      size: 50,
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
      .filter(
        (nit): nit is Nit =>
          Boolean(nit),
      );
    onSelectionChange(selected);
  }, [onSelectionChange, rowSelection, data]);

  return (
    <div className="overflow-hidden rounded-lg border border-border bg-card">
      <Table>
        <TableHeader>
          {table.getHeaderGroups().map((headerGroup) => (
            <TableRow key={headerGroup.id} className="hover:bg-transparent">
              {headerGroup.headers.map((header) => (
                <TableHead
                  key={header.id}
                  className="h-11 text-xs font-medium text-muted-foreground"
                >
                  {header.isPlaceholder
                    ? null
                    : flexRender(
                        header.column.columnDef.header,
                        header.getContext(),
                      )}
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
                  <TableCell key={cell.id} className="py-3 align-middle">
                    {flexRender(cell.column.columnDef.cell, cell.getContext())}
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
  );
}
