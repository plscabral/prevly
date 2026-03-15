'use client'

import { useState } from 'react'
import { useRouter } from 'next/navigation'
import { Check, ChevronsUpDown, ArrowLeft } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Field, FieldLabel, FieldGroup, FieldError } from '@/components/ui/field'
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover'
import { Command, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList } from '@/components/ui/command'
import { cn } from '@/lib/utils'
import { availableNITs } from '@/lib/store'
import { toast } from 'sonner'
import Link from 'next/link'

interface FormData {
  name: string
  document: string
  age: string
  govPassword: string
  nitNumber: string
}

interface FormErrors {
  name?: string
  document?: string
  age?: string
  govPassword?: string
}

export function PersonForm() {
  const router = useRouter()
  const [open, setOpen] = useState(false)
  const [formData, setFormData] = useState<FormData>({
    name: '',
    document: '',
    age: '',
    govPassword: '',
    nitNumber: '',
  })
  const [errors, setErrors] = useState<FormErrors>({})
  const [isSubmitting, setIsSubmitting] = useState(false)

  const formatCPF = (value: string) => {
    const numbers = value.replace(/\D/g, '')
    if (numbers.length <= 3) return numbers
    if (numbers.length <= 6) return `${numbers.slice(0, 3)}.${numbers.slice(3)}`
    if (numbers.length <= 9) return `${numbers.slice(0, 3)}.${numbers.slice(3, 6)}.${numbers.slice(6)}`
    return `${numbers.slice(0, 3)}.${numbers.slice(3, 6)}.${numbers.slice(6, 9)}-${numbers.slice(9, 11)}`
  }

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target
    
    if (name === 'document') {
      setFormData(prev => ({ ...prev, [name]: formatCPF(value) }))
    } else if (name === 'age') {
      const numbers = value.replace(/\D/g, '')
      setFormData(prev => ({ ...prev, [name]: numbers }))
    } else {
      setFormData(prev => ({ ...prev, [name]: value }))
    }
    
    if (errors[name as keyof FormErrors]) {
      setErrors(prev => ({ ...prev, [name]: undefined }))
    }
  }

  const validate = () => {
    const newErrors: FormErrors = {}
    
    if (!formData.name.trim()) {
      newErrors.name = 'Nome e obrigatorio'
    }
    
    if (!formData.document.trim()) {
      newErrors.document = 'CPF e obrigatorio'
    } else if (formData.document.replace(/\D/g, '').length !== 11) {
      newErrors.document = 'CPF invalido'
    }
    
    if (!formData.age.trim()) {
      newErrors.age = 'Idade e obrigatoria'
    } else if (parseInt(formData.age) < 1 || parseInt(formData.age) > 120) {
      newErrors.age = 'Idade invalida'
    }
    
    if (!formData.govPassword.trim()) {
      newErrors.govPassword = 'Senha e obrigatoria'
    } else if (formData.govPassword.length < 6) {
      newErrors.govPassword = 'Senha deve ter no minimo 6 caracteres'
    }
    
    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    
    if (!validate()) return
    
    setIsSubmitting(true)
    
    await new Promise(resolve => setTimeout(resolve, 1000))
    
    toast.success('Pessoa cadastrada com sucesso!')
    router.push('/pessoas')
  }

  const nitOptions = availableNITs.map(nit => ({
    value: nit.number,
    label: nit.number,
    years: nit.contributionYears,
  }))

  return (
    <div className="flex flex-col gap-6">
      {/* Header */}
      <div>
        <Link 
          href="/pessoas" 
          className="mb-4 inline-flex items-center gap-1.5 text-sm text-muted-foreground hover:text-foreground transition-colors"
        >
          <ArrowLeft className="h-4 w-4" />
          Voltar para Pessoas
        </Link>
        <h1 className="text-2xl font-semibold tracking-tight text-foreground">Nova Pessoa</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Preencha as informacoes para cadastrar uma nova pessoa
        </p>
      </div>

      {/* Form */}
      <div className="rounded-lg border border-border bg-card p-6">
        <form onSubmit={handleSubmit} className="space-y-5">
          <FieldGroup>
            <Field data-invalid={!!errors.name}>
              <FieldLabel htmlFor="name">Nome Completo</FieldLabel>
              <Input
                id="name"
                name="name"
                placeholder="Digite o nome completo"
                value={formData.name}
                onChange={handleChange}
                className="bg-background"
                aria-invalid={!!errors.name}
              />
              {errors.name && <FieldError>{errors.name}</FieldError>}
            </Field>

            <div className="grid gap-5 sm:grid-cols-2">
              <Field data-invalid={!!errors.document}>
                <FieldLabel htmlFor="document">CPF</FieldLabel>
                <Input
                  id="document"
                  name="document"
                  placeholder="000.000.000-00"
                  value={formData.document}
                  onChange={handleChange}
                  maxLength={14}
                  className="bg-background font-mono"
                  aria-invalid={!!errors.document}
                />
                {errors.document && <FieldError>{errors.document}</FieldError>}
              </Field>

              <Field data-invalid={!!errors.age}>
                <FieldLabel htmlFor="age">Idade</FieldLabel>
                <Input
                  id="age"
                  name="age"
                  placeholder="Ex: 58"
                  value={formData.age}
                  onChange={handleChange}
                  maxLength={3}
                  className="bg-background"
                  aria-invalid={!!errors.age}
                />
                {errors.age && <FieldError>{errors.age}</FieldError>}
              </Field>
            </div>

            <Field data-invalid={!!errors.govPassword}>
              <FieldLabel htmlFor="govPassword">Senha Gov.br</FieldLabel>
              <Input
                id="govPassword"
                name="govPassword"
                type="password"
                placeholder="Senha de acesso ao Gov.br"
                value={formData.govPassword}
                onChange={handleChange}
                className="bg-background"
                aria-invalid={!!errors.govPassword}
              />
              {errors.govPassword && <FieldError>{errors.govPassword}</FieldError>}
            </Field>

            <Field>
              <FieldLabel>NIT (Opcional)</FieldLabel>
              <Popover open={open} onOpenChange={setOpen}>
                <PopoverTrigger asChild>
                  <Button
                    variant="outline"
                    role="combobox"
                    aria-expanded={open}
                    className="w-full justify-between font-normal bg-background"
                  >
                    {formData.nitNumber
                      ? nitOptions.find((nit) => nit.value === formData.nitNumber)?.label
                      : "Selecione um NIT disponivel..."}
                    <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
                  </Button>
                </PopoverTrigger>
                <PopoverContent className="w-[400px] p-0" align="start">
                  <Command>
                    <CommandInput placeholder="Buscar NIT..." />
                    <CommandList>
                      <CommandEmpty>Nenhum NIT encontrado.</CommandEmpty>
                      <CommandGroup>
                        {nitOptions.map((nit) => (
                          <CommandItem
                            key={nit.value}
                            value={nit.value}
                            onSelect={(currentValue) => {
                              setFormData(prev => ({
                                ...prev,
                                nitNumber: currentValue === formData.nitNumber ? '' : currentValue,
                              }))
                              setOpen(false)
                            }}
                          >
                            <Check
                              className={cn(
                                "mr-2 h-4 w-4",
                                formData.nitNumber === nit.value ? "opacity-100" : "opacity-0"
                              )}
                            />
                            <div className="flex flex-1 items-center justify-between">
                              <span className="font-mono">{nit.label}</span>
                              <span className="text-xs text-muted-foreground">
                                {nit.years} anos de contribuicao
                              </span>
                            </div>
                          </CommandItem>
                        ))}
                      </CommandGroup>
                    </CommandList>
                  </Command>
                </PopoverContent>
              </Popover>
              <p className="mt-1.5 text-xs text-muted-foreground">
                Vincule um NIT existente a esta pessoa
              </p>
            </Field>
          </FieldGroup>

          <div className="flex items-center gap-3 pt-2 border-t border-border">
            <Button type="submit" disabled={isSubmitting} className="mt-4">
              {isSubmitting ? 'Salvando...' : 'Cadastrar Pessoa'}
            </Button>
            <Button
              type="button"
              variant="ghost"
              onClick={() => router.push('/pessoas')}
              className="mt-4"
            >
              Cancelar
            </Button>
          </div>
        </form>
      </div>
    </div>
  )
}
