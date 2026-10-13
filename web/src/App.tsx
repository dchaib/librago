import { useEffect, useState } from 'react'
import { Link, NavLink, Route, Routes } from 'react-router'
import { Button, buttonVariants } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardFooter, CardHeader } from '@/components/ui/card'
import { Input } from '@/components/ui/input'

function App() {
  const [count, setCount] = useState(0)
  const [theme, setTheme] = useState<'ocean' | 'natural'>('ocean')
  const [mode, setMode] = useState<'light' | 'dark'>('light')

  useEffect(() => {
    document.documentElement.dataset.theme = theme
    document.documentElement.classList.toggle('dark', mode === 'dark')
  }, [theme, mode])

  return (
    <main className="mx-auto max-w-3xl space-y-6 px-4 py-8 sm:px-6">
      <header className="space-y-2">
        <p className="text-sm text-muted-foreground">Aperçu de l’interface · développement</p>
        <h1 className="font-heading text-3xl font-semibold">Librago</h1>
        <p className="text-muted-foreground">
          Deux ambiances, une même interface. Testez les couleurs et les interactions.
        </p>
      </header>

      <div className="flex flex-wrap gap-x-8 gap-y-4">
        <fieldset className="space-y-2">
          <legend className="text-sm font-medium">Thème</legend>
          <div className="flex gap-2">
            <Button variant={theme === 'ocean' ? 'default' : 'secondary'} aria-pressed={theme === 'ocean'} onClick={() => setTheme('ocean')}>Océan</Button>
            <Button variant={theme === 'natural' ? 'default' : 'secondary'} aria-pressed={theme === 'natural'} onClick={() => setTheme('natural')}>Naturel</Button>
          </div>
        </fieldset>
        <fieldset className="space-y-2">
          <legend className="text-sm font-medium">Mode</legend>
          <div className="flex gap-2">
            <Button variant={mode === 'light' ? 'default' : 'secondary'} aria-pressed={mode === 'light'} onClick={() => setMode('light')}>Clair</Button>
            <Button variant={mode === 'dark' ? 'default' : 'secondary'} aria-pressed={mode === 'dark'} onClick={() => setMode('dark')}>Sombre</Button>
          </div>
        </fieldset>
      </div>

      <nav aria-label="Navigation de l’aperçu" className="flex gap-4 text-sm">
        <NavLink className="rounded-sm text-primary underline underline-offset-4 focus-visible:outline-2 focus-visible:outline-offset-4 aria-[current=page]:font-semibold" to="/">Accueil</NavLink>
        <NavLink className="rounded-sm text-primary underline underline-offset-4 focus-visible:outline-2 focus-visible:outline-offset-4 aria-[current=page]:font-semibold" to="/verification">Vérification</NavLink>
      </nav>

      <Routes>
        <Route path="/" element={
          <Card>
            <CardHeader>
              <h2 className="font-heading text-xl font-medium">Une surface pour le contenu</h2>
              <CardDescription>Texte secondaire, champ de saisie et actions shadcn avec Base UI.</CardDescription>
            </CardHeader>
            <CardContent className="space-y-5">
              <div className="space-y-2">
                <label htmlFor="preview-text" className="text-sm font-medium">Texte d’essai</label>
                <Input id="preview-text" aria-describedby="preview-text-help" placeholder="Écrivez quelques mots…" />
                <p id="preview-text-help" className="text-sm text-muted-foreground">Champ libre pour vérifier la lisibilité et le focus clavier.</p>
              </div>
              <div className="flex flex-wrap gap-2">
                <Button onClick={() => setCount((value) => value + 1)}>Tester le bouton</Button>
                <Button variant="secondary" onClick={() => setCount(0)}>Réinitialiser</Button>
                <Button disabled>Indisponible</Button>
              </div>
              <Link className={buttonVariants({ variant: 'link' })} to="/verification">Voir la vérification</Link>
              <section aria-labelledby="status-title" className="space-y-2">
                <h3 id="status-title" className="text-sm font-medium">Exemples de statuts</h3>
                <ul className="grid gap-2 text-sm sm:grid-cols-2">
                  <li className="rounded-lg bg-success px-3 py-2 text-success-foreground">Succès · Opération terminée</li>
                  <li className="rounded-lg bg-info px-3 py-2 text-info-foreground">Information · Message d’exemple</li>
                  <li className="rounded-lg bg-warning px-3 py-2 text-warning-foreground">Attention · À vérifier</li>
                  <li className="rounded-lg bg-danger px-3 py-2 text-danger-foreground">Erreur · Action impossible</li>
                </ul>
              </section>
            </CardContent>
            <CardFooter>
              <p role="status" aria-live="polite">Nombre de clics : {count}</p>
            </CardFooter>
          </Card>
        } />
        <Route path="/verification" element={
          <Card>
            <CardHeader>
              <h2 className="font-heading text-xl font-medium">Navigation réussie</h2>
              <CardDescription>React Router affiche cette seconde page sans recharger le document.</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <p>Le thème et le mode restent sélectionnés pendant la navigation.</p>
              <Link className={buttonVariants()} to="/">Revenir à l’accueil</Link>
            </CardContent>
          </Card>
        } />
      </Routes>
    </main>
  )
}

export default App
