import { useState } from 'react'
import { Link, Route, Routes } from 'react-router'
import { Button } from '@/components/ui/button'

function App() {
  const [count, setCount] = useState(0)

  return (
    <main className="mx-auto max-w-xl space-y-6 p-6">
      <header className="space-y-2">
        <h1 className="text-2xl font-semibold">Librago — aperçu React</h1>
        <p className="text-sm text-muted-foreground">
          Validation technique de l’interface, réservée au développement.
        </p>
      </header>
      <nav aria-label="Navigation de l’aperçu" className="flex gap-4">
        <Link className="underline underline-offset-4" to="/">Accueil</Link>
        <Link className="underline underline-offset-4" to="/verification">Vérification</Link>
      </nav>
      <Routes>
        <Route path="/" element={
          <section className="space-y-4 rounded-lg border p-4">
            <h2 className="text-lg font-medium">React fonctionne</h2>
            <p>Ce bouton shadcn utilise les primitives Base UI.</p>
            <Button onClick={() => setCount((value) => value + 1)}>Tester le bouton</Button>
            <p role="status">Nombre de clics : {count}</p>
          </section>
        } />
        <Route path="/verification" element={
          <section className="space-y-2 rounded-lg border p-4">
            <h2 className="text-lg font-medium">Navigation réussie</h2>
            <p>React Router affiche cette seconde page sans recharger le document.</p>
            <p className="text-sm text-muted-foreground">Les espacements, la bordure et la typographie utilisent Tailwind CSS.</p>
          </section>
        } />
      </Routes>
    </main>
  )
}

export default App
