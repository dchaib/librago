import { BookOpenIcon, BookmarkIcon, HouseIcon } from 'lucide-react'
import { Link, NavLink, Outlet } from 'react-router'
import { AppearanceControls } from '@/components/AppearanceControls'
import { cn } from '@/lib/utils'

const destinations = [
  { to: '/', label: 'Accueil', icon: HouseIcon },
  { to: '/loans', label: 'Prêts', icon: BookOpenIcon },
  { to: '/reservations', label: 'Réservations', icon: BookmarkIcon },
]

function PrimaryNavigation({ mobile = false }: { mobile?: boolean }) {
  return <nav
    aria-label="Navigation principale"
    className={mobile
      ? 'fixed inset-x-0 bottom-0 z-20 border-t bg-background pb-[env(safe-area-inset-bottom)] pl-[env(safe-area-inset-left)] pr-[env(safe-area-inset-right)] md:hidden'
      : 'hidden md:block'}
  >
    <ul className={mobile ? 'grid grid-cols-3 gap-1 px-3 py-2' : 'flex gap-1'}>
      {destinations.map(({ to, label, icon: Icon }) => <li key={to}>
        <NavLink
          to={to}
          end
          className={({ isActive }) => cn(
            'flex min-h-11 items-center justify-center rounded-lg text-sm text-foreground focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring',
            mobile ? 'min-h-14 flex-col gap-1 px-1 py-1 text-xs' : 'px-3',
            isActive ? 'bg-secondary font-semibold text-secondary-foreground' : 'hover:bg-muted',
          )}
        >
          {mobile && <Icon aria-hidden="true" className="size-5" />}
          {label}
        </NavLink>
      </li>)}
    </ul>
  </nav>
}

export function AppShell() {
  return <div className="min-h-dvh">
    <a href="#main-content" className="sr-only z-30 rounded-lg bg-background p-3 text-foreground focus:not-sr-only focus:absolute focus:top-2 focus:left-3 focus:outline-2 focus:outline-ring">Aller au contenu</a>
    <header className="border-b">
      <div className="mx-auto flex max-w-5xl items-center gap-2 px-3 py-4 sm:px-6 md:gap-6">
        <Link to="/" aria-label="Librago — Accueil" className="flex min-h-11 shrink-0 items-center rounded-sm focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring">
          <img src="/brand/librago-logo-horizontal.svg" alt="Librago" width="842.272" height="220.4343" className="h-auto w-32 sm:w-40 dark:hidden" />
          <img src="/brand/librago-logo-horizontal-on-dark.svg" alt="Librago" width="842.272" height="220.4343" className="hidden h-auto w-32 sm:w-40 dark:block" />
        </Link>
        <PrimaryNavigation />
        <AppearanceControls />
      </div>
    </header>
    <main id="main-content" tabIndex={-1} className="mx-auto max-w-5xl px-3 pt-6 pb-[calc(6rem+env(safe-area-inset-bottom))] focus:outline-none sm:px-6 md:pb-6">
      <Outlet />
    </main>
    <PrimaryNavigation mobile />
  </div>
}
