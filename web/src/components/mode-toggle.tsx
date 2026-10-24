import { useEffect, useState, useSyncExternalStore } from "react"
import { Moon, Sun } from "lucide-react"

import { Button } from "@/components/ui/button"

const colorSchemeQuery = "(prefers-color-scheme: dark)"

function subscribeToColorScheme(onChange: () => void) {
  const mediaQuery = window.matchMedia(colorSchemeQuery)
  mediaQuery.addEventListener("change", onChange)
  return () => mediaQuery.removeEventListener("change", onChange)
}

function systemDarkMode() {
  return window.matchMedia(colorSchemeQuery).matches
}

export function ModeToggle() {
  const systemDark = useSyncExternalStore(
    subscribeToColorScheme,
    systemDarkMode
  )
  const [override, setOverride] = useState<boolean | null>(null)
  const dark = override ?? systemDark
  const label = dark ? "Passer en mode clair" : "Passer en mode sombre"

  useEffect(() => {
    const root = document.documentElement
    root.classList.toggle("dark", dark)
    root.classList.toggle("light", !dark)
    root.style.colorScheme = dark ? "dark" : "light"
  }, [dark])

  return (
    <Button
      variant="ghost"
      size="icon"
      className="ml-auto"
      aria-label={label}
      title={label}
      onClick={() => setOverride(!dark)}
    >
      {dark ? <Sun aria-hidden="true" /> : <Moon aria-hidden="true" />}
    </Button>
  )
}
