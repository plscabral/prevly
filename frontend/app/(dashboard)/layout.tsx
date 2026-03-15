import { AppHeader } from '@/components/app-header'

export default function DashboardLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return (
    <div className="min-h-screen bg-background">
      <AppHeader />
      <main>
        <div className="mx-auto max-w-6xl px-6 py-8">
          {children}
        </div>
      </main>
    </div>
  )
}
