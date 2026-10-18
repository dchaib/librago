import { useEffect, useState } from 'react'
import { MoonIcon, SunIcon } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Select, SelectContent, SelectGroup, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'

const themes = [
  { label: 'Océan', value: 'ocean' },
  { label: 'Naturel', value: 'natural' },
]

export function AppearanceControls() {
  const [theme, setTheme] = useState<'ocean' | 'natural'>('ocean')
  const [mode, setMode] = useState<'light' | 'dark'>('light')

  useEffect(() => {
    document.documentElement.dataset.theme = theme
    document.documentElement.classList.toggle('dark', mode === 'dark')
  }, [theme, mode])

  return <div className="ml-auto flex shrink-0 items-center gap-2">
    <Select items={themes} value={theme} onValueChange={(value) => setTheme(value === 'natural' ? 'natural' : 'ocean')}>
      <SelectTrigger aria-label="Thème" className="w-26 data-[size=default]:h-11">
        <SelectValue />
      </SelectTrigger>
      <SelectContent alignItemWithTrigger={false}>
        <SelectGroup>
          {themes.map((item) => <SelectItem key={item.value} value={item.value}>{item.label}</SelectItem>)}
        </SelectGroup>
      </SelectContent>
    </Select>
    <Button
      variant="outline"
      size="icon"
      className="size-11 focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-offset-2 focus-visible:outline-ring"
      aria-label="Mode sombre"
      aria-pressed={mode === 'dark'}
      title={mode === 'dark' ? 'Passer en mode clair' : 'Passer en mode sombre'}
      onClick={() => setMode(mode === 'light' ? 'dark' : 'light')}
    >
      {mode === 'dark' ? <MoonIcon aria-hidden="true" /> : <SunIcon aria-hidden="true" />}
    </Button>
  </div>
}
