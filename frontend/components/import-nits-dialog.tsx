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
  usePostApiSocialSecurityRegistrationImportPdf,
} from "@/lib/api/generated/social-security-registration/social-security-registration";
import { ImportSocialSecurityRegistrationsResultDto } from "@/lib/api/generated/model";

interface ImportNitsDialogProps {
  onClose: () => void;
}

export function ImportNitsDialog({ onClose }: ImportNitsDialogProps) {
  const queryClient = useQueryClient();
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const importPdfMutation = usePostApiSocialSecurityRegistrationImportPdf();

  const onDrop = useCallback((acceptedFiles: File[]) => {
    if (!acceptedFiles.length) return;
    setSelectedFile(acceptedFiles[0]);
  }, []);

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    multiple: false,
    accept: { "application/pdf": [".pdf"] },
  });

  const onImportPdf = async () => {
    if (!selectedFile) {
      toast.error("Selecione um arquivo PDF para importar.");
      return;
    }

    try {
      const response = await importPdfMutation.mutateAsync({
        data: { File: selectedFile },
      });

      await queryClient.invalidateQueries({
        queryKey: getGetApiSocialSecurityRegistrationQueryKey(),
      });

      const result = response.data as ImportSocialSecurityRegistrationsResultDto;
      toast.success(
        `Importação concluída. Inseridos: ${result.inserted ?? 0}, duplicados: ${result.duplicates ?? 0}.`,
      );
      onClose();
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Erro ao importar PDF.");
    }
  };

  return (
    <div className="space-y-4">
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
          {isDragActive ? "Solte o PDF aqui..." : "Arraste um PDF ou clique para selecionar"}
        </p>
        <p className="mt-1 text-xs text-muted-foreground">
          Apenas PDF. O sistema vai extrair os NITs automaticamente.
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
            disabled={importPdfMutation.isPending}
          >
            <X className="h-4 w-4" />
          </Button>
        </div>
      )}

      <div className="flex justify-end gap-2">
        <Button type="button" variant="outline" onClick={onClose} disabled={importPdfMutation.isPending}>
          Cancelar
        </Button>
        <Button type="button" onClick={onImportPdf} disabled={!selectedFile || importPdfMutation.isPending}>
          {importPdfMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
          Importar PDF
        </Button>
      </div>
    </div>
  );
}
