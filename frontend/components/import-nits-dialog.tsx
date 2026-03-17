"use client";

import { useCallback, useState } from "react";
import { FileText, Loader2, Upload, X } from "lucide-react";
import { useDropzone } from "react-dropzone";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import {
  getGetApiSocialSecurityRegistrationQueryKey,
  usePostApiSocialSecurityRegistrationImportContributionDetails,
  usePostApiSocialSecurityRegistrationImportPdf,
} from "@/lib/api/generated/social-security-registration/social-security-registration";
import {
  ContributionDetailsImportResultDto,
  ImportSocialSecurityRegistrationsResultDto,
} from "@/lib/api/generated/model";
import { Label } from "@/components/ui/label";
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";

interface ImportNitsDialogProps {
  onClose: () => void;
}

type ImportFlow = "nit-check" | "nit-detail";

export function ImportNitsDialog({ onClose }: ImportNitsDialogProps) {
  const queryClient = useQueryClient();
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [flow, setFlow] = useState<ImportFlow>("nit-check");
  const importPdfMutation = usePostApiSocialSecurityRegistrationImportPdf();
  const importContributionDetailsMutation =
    usePostApiSocialSecurityRegistrationImportContributionDetails();
  const isImporting =
    importPdfMutation.isPending || importContributionDetailsMutation.isPending;

  const onDrop = useCallback((acceptedFiles: File[]) => {
    if (!acceptedFiles.length) return;
    setSelectedFile(acceptedFiles[0]);
  }, []);

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    multiple: false,
    accept: {
      "application/pdf": [".pdf"],
      "application/zip": [".zip"],
      "application/x-zip-compressed": [".zip"],
      "application/vnd.rar": [".rar"],
      "application/x-rar-compressed": [".rar"],
    },
  });

  const onImport = async () => {
    if (!selectedFile) {
      toast.error("Selecione um arquivo .pdf, .zip ou .rar para importar.");
      return;
    }

    try {
      if (flow === "nit-check") {
        const response = await importPdfMutation.mutateAsync({
          data: { File: selectedFile },
        });

        const result = response.data as ImportSocialSecurityRegistrationsResultDto;
        toast.success(
          `Checagem concluída. Inseridos: ${result.inserted ?? 0}, duplicados: ${result.duplicates ?? 0}.`,
        );
      } else {
        const response = await importContributionDetailsMutation.mutateAsync({
          data: { files: [selectedFile] },
        });

        const result = response.data as ContributionDetailsImportResultDto;
        toast.success(
          `Detalhes concluídos. Atualizados: ${result.updatedRegistrations ?? 0}, não encontrados: ${result.notFoundNits ?? 0}.`,
        );
      }

      await queryClient.invalidateQueries({
        queryKey: getGetApiSocialSecurityRegistrationQueryKey(),
      });

      onClose();
    } catch (error) {
      toast.error(
        error instanceof Error ? error.message : "Erro ao processar importação.",
      );
    }
  };

  return (
    <div className="space-y-4">
      <div className="space-y-2">
        <Label>Fluxo de processamento</Label>
        <RadioGroup
          value={flow}
          onValueChange={(value) => setFlow(value as ImportFlow)}
          className="grid gap-2"
        >
          <div className="flex items-start gap-3 rounded-md border p-3">
            <RadioGroupItem value="nit-check" id="flow-nit-check" className="mt-0.5" />
            <div className="space-y-1">
              <Label htmlFor="flow-nit-check" className="cursor-pointer">
                Checagem de NITs
              </Label>
              <p className="text-xs text-muted-foreground">
                Lê os PDFs e salva os NITs.
              </p>
            </div>
          </div>
          <div className="flex items-start gap-3 rounded-md border p-3">
            <RadioGroupItem value="nit-detail" id="flow-nit-detail" className="mt-0.5" />
            <div className="space-y-1">
              <Label htmlFor="flow-nit-detail" className="cursor-pointer">
                Obter detalhe das NITs
              </Label>
              <p className="text-xs text-muted-foreground">
                Extrai NIT e atualiza primeira/última contribuição.
              </p>
            </div>
          </div>
        </RadioGroup>
      </div>

      <div
        {...getRootProps()}
        className={cn(
          "cursor-pointer rounded-lg border border-dashed p-8 text-center transition-colors",
          isDragActive ? "border-primary bg-primary/5" : "border-border bg-muted/30",
        )}
      >
        <input {...getInputProps()} />
        <Upload className="mx-auto h-8 w-8 text-muted-foreground" />
        <p className="mt-3 text-sm font-medium">
          {isDragActive
            ? "Solte o arquivo aqui..."
            : "Arraste um arquivo ou clique para selecionar"}
        </p>
        <p className="mt-1 text-xs text-muted-foreground">
          Formatos aceitos: PDF, ZIP e RAR.
        </p>
      </div>

      {selectedFile && (
        <div className="flex items-center justify-between rounded-md border bg-card p-3">
          <div className="flex items-center gap-2">
            <FileText className="h-4 w-4 text-muted-foreground" />
            <span className="text-sm">{selectedFile.name}</span>
          </div>
          <Button
            type="button"
            variant="ghost"
            size="icon"
            onClick={() => setSelectedFile(null)}
            disabled={isImporting}
          >
            <X className="h-4 w-4" />
          </Button>
        </div>
      )}

      <div className="flex justify-end gap-2">
        <Button type="button" variant="outline" onClick={onClose} disabled={isImporting}>
          Cancelar
        </Button>
        <Button type="button" onClick={onImport} disabled={!selectedFile || isImporting}>
          {isImporting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
          {flow === "nit-check" ? "Executar checagem" : "Executar detalhes"}
        </Button>
      </div>
    </div>
  );
}
