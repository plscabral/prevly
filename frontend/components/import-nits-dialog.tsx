'use client'

import { useState, useCallback } from 'react'
import { useDropzone } from 'react-dropzone'
import { Upload, FileText, Check, X, Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'
import { toast } from 'sonner'

interface ExtractedNIT {
  number: string
  firstContributionDate: string
  lastContributionDate: string
  contributionYears: number
  selected: boolean
}

interface ImportNitsDialogProps {
  onClose: () => void
}

export function ImportNitsDialog({ onClose }: ImportNitsDialogProps) {
  const [file, setFile] = useState<File | null>(null)
  const [extractedNITs, setExtractedNITs] = useState<ExtractedNIT[]>([])
  const [isProcessing, setIsProcessing] = useState(false)
  const [isImporting, setIsImporting] = useState(false)

  const onDrop = useCallback((acceptedFiles: File[]) => {
    const pdfFile = acceptedFiles[0]
    if (pdfFile && pdfFile.type === 'application/pdf') {
      setFile(pdfFile)
      processFile(pdfFile)
    }
  }, [])

  const processFile = async (file: File) => {
    setIsProcessing(true)
    // Simula extração de NITs do PDF
    await new Promise(resolve => setTimeout(resolve, 1500))
    
    // Mock extracted NITs
    setExtractedNITs([
      {
        number: '33344455566',
        firstContributionDate: '2005-03-10',
        lastContributionDate: '2024-02-15',
        contributionYears: 19,
        selected: true,
      },
      {
        number: '77788899900',
        firstContributionDate: '1998-07-22',
        lastContributionDate: '2024-01-30',
        contributionYears: 26,
        selected: true,
      },
      {
        number: '44455566677',
        firstContributionDate: '2010-11-05',
        lastContributionDate: '2023-12-31',
        contributionYears: 13,
        selected: true,
      },
    ])
    setIsProcessing(false)
  }

  const toggleNIT = (index: number) => {
    setExtractedNITs(prev => prev.map((nit, i) => 
      i === index ? { ...nit, selected: !nit.selected } : nit
    ))
  }

  const handleImport = async () => {
    const selectedNITs = extractedNITs.filter(nit => nit.selected)
    if (selectedNITs.length === 0) {
      toast.error('Selecione pelo menos um NIT para importar')
      return
    }

    setIsImporting(true)
    await new Promise(resolve => setTimeout(resolve, 1000))
    
    toast.success(`${selectedNITs.length} NIT(s) importado(s) com sucesso`)
    setIsImporting(false)
    onClose()
  }

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept: { 'application/pdf': ['.pdf'] },
    maxFiles: 1,
  })

  const selectedCount = extractedNITs.filter(nit => nit.selected).length

  return (
    <div className="space-y-6">
      {/* Dropzone */}
      <div
        {...getRootProps()}
        className={cn(
          'flex flex-col items-center justify-center rounded-lg border-2 border-dashed p-8 transition-colors cursor-pointer',
          isDragActive 
            ? 'border-foreground bg-accent' 
            : 'border-border hover:border-muted-foreground hover:bg-accent/50',
          file && 'border-emerald-500 bg-emerald-50'
        )}
      >
        <input {...getInputProps()} />
        {isProcessing ? (
          <>
            <Loader2 className="h-10 w-10 animate-spin text-muted-foreground" />
            <p className="mt-3 text-sm text-muted-foreground">Processando PDF...</p>
          </>
        ) : file ? (
          <>
            <FileText className="h-10 w-10 text-emerald-600" />
            <p className="mt-3 text-sm font-medium text-foreground">{file.name}</p>
            <p className="text-xs text-muted-foreground">
              {(file.size / 1024).toFixed(1)} KB
            </p>
          </>
        ) : (
          <>
            <Upload className="h-10 w-10 text-muted-foreground" />
            <p className="mt-3 text-sm text-foreground">
              {isDragActive ? 'Solte o arquivo aqui' : 'Arraste um PDF ou clique para selecionar'}
            </p>
            <p className="mt-1 text-xs text-muted-foreground">
              Apenas arquivos PDF são aceitos
            </p>
          </>
        )}
      </div>

      {/* Extracted NITs */}
      {extractedNITs.length > 0 && (
        <div className="space-y-3">
          <div className="flex items-center justify-between">
            <h3 className="text-sm font-medium text-foreground">
              NITs Encontrados ({extractedNITs.length})
            </h3>
            <span className="text-xs text-muted-foreground">
              {selectedCount} selecionado(s)
            </span>
          </div>

          <div className="max-h-64 space-y-2 overflow-y-auto rounded-lg border border-border">
            {extractedNITs.map((nit, index) => (
              <div
                key={nit.number}
                onClick={() => toggleNIT(index)}
                className={cn(
                  'flex items-center justify-between p-3 cursor-pointer transition-colors',
                  index !== 0 && 'border-t border-border',
                  nit.selected ? 'bg-accent/50' : 'hover:bg-accent/30'
                )}
              >
                <div className="flex items-center gap-3">
                  <div className={cn(
                    'flex h-5 w-5 items-center justify-center rounded border transition-colors',
                    nit.selected 
                      ? 'border-foreground bg-foreground' 
                      : 'border-muted-foreground'
                  )}>
                    {nit.selected && <Check className="h-3 w-3 text-background" />}
                  </div>
                  <div>
                    <p className="font-mono text-sm font-medium text-foreground">{nit.number}</p>
                    <p className="text-xs text-muted-foreground">
                      {nit.contributionYears} anos de contribuição
                    </p>
                  </div>
                </div>
                <div className="text-right text-xs text-muted-foreground">
                  <p>{new Date(nit.firstContributionDate).toLocaleDateString('pt-BR')}</p>
                  <p>a {new Date(nit.lastContributionDate).toLocaleDateString('pt-BR')}</p>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Actions */}
      <div className="flex justify-end gap-3">
        <Button variant="outline" onClick={onClose}>
          Cancelar
        </Button>
        <Button 
          onClick={handleImport} 
          disabled={selectedCount === 0 || isImporting}
        >
          {isImporting ? (
            <>
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              Importando...
            </>
          ) : (
            <>Importar {selectedCount > 0 ? `(${selectedCount})` : ''}</>
          )}
        </Button>
      </div>
    </div>
  )
}
