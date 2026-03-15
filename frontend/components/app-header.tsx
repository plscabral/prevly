'use client'

import Link from 'next/link'
import { usePathname } from 'next/navigation'
import { cn } from '@/lib/utils'
import { ThemeToggle } from './theme-toggle'

const navigation = [
  { name: 'Dashboard', href: '/' },
  { name: 'Pessoas', href: '/pessoas' },
  { name: 'NITs', href: '/nits' },
]

export function AppHeader() {
  const pathname = usePathname()

  return (
    <header className="sticky top-0 z-50 w-full border-b border-border bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
      <div className="mx-auto flex h-14 max-w-6xl items-center justify-between px-6">
        <div className="flex items-center gap-8">
          {/* Logo */}
          <Link href="/" className="flex items-center gap-2.5">
            <div className="flex h-7 w-7 items-center justify-center rounded-md bg-foreground">
              <span className="text-xs font-bold text-background">P</span>
            </div>
            <span className="text-base font-semibold tracking-tight">Prevly</span>
          </Link>

          {/* Navigation */}
          <nav className="flex items-center gap-1">
            {navigation.map((item) => {
              const isActive = item.href === '/' 
                ? pathname === '/'
                : pathname.startsWith(item.href)
              
              return (
                <Link
                  key={item.name}
                  href={item.href}
                  className={cn(
                    'px-3 py-1.5 text-sm font-medium rounded-md transition-colors',
                    isActive
                      ? 'bg-accent text-foreground'
                      : 'text-muted-foreground hover:text-foreground hover:bg-accent/50'
                  )}
                >
                  {item.name}
                </Link>
              )
            })}
          </nav>
        </div>

        {/* Theme Toggle */}
        <ThemeToggle />
      </div>
    </header>
  )
}
