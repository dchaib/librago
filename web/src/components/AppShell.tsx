import { Link, NavLink, Outlet } from "react-router"

import { buttonVariants } from "@/components/ui/button"
import { ModeToggle } from "@/components/mode-toggle"

const destinations = [
  { to: "/", label: "Accueil" },
  { to: "/loans", label: "Prêts" },
  { to: "/reservations", label: "Réservations" },
]

export function AppShell() {
  return (
    <div className="min-h-svh">
      <a
        href="#main-content"
        className="sr-only focus:not-sr-only focus:absolute focus:z-10 focus:bg-background focus:p-3"
      >
        Aller au contenu
      </a>
      <header className="border-b">
        <div className="mx-auto flex max-w-5xl flex-wrap items-center gap-4 px-4 py-3 sm:px-6">
          <Link
            to="/"
            aria-label="Librago — Accueil"
            className={buttonVariants({ variant: "ghost", size: "icon" })}
          >
            <img
              src="/brand/librago-symbol.svg"
              alt=""
              width="32"
              height="32"
            />
          </Link>
          <nav aria-label="Navigation principale">
            <ul className="flex flex-wrap gap-1">
              {destinations.map(({ to, label }) => (
                <li key={to}>
                  <NavLink
                    to={to}
                    end
                    className={({ isActive }) =>
                      buttonVariants({
                        variant: isActive ? "secondary" : "ghost",
                      })
                    }
                  >
                    {label}
                  </NavLink>
                </li>
              ))}
            </ul>
          </nav>
          <ModeToggle />
        </div>
      </header>
      <main
        id="main-content"
        tabIndex={-1}
        className="mx-auto max-w-5xl px-4 py-6 sm:px-6"
      >
        <Outlet />
      </main>
    </div>
  )
}
