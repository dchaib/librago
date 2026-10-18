import { Link, Route, Routes } from 'react-router'
import { AppShell } from '@/components/AppShell'
import { buttonVariants } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader } from '@/components/ui/card'
import { LoansPage } from '@/loans/LoansPage'

function PlaceholderPage({ title, message }: { title: string; message: string }) {
  return <div className="space-y-3">
    <h1 className="font-heading text-3xl font-semibold sm:text-4xl">{title}</h1>
    <p className="text-muted-foreground">{message}</p>
  </div>
}

function App() {
  return <Routes>
    <Route element={<AppShell />}>
      <Route index element={<PlaceholderPage title="Accueil" message="Le tableau de bord n’est pas encore disponible." />} />
      <Route path="/loans" element={<LoansPage />} />
      <Route path="/reservations" element={<PlaceholderPage title="Réservations" message="Le suivi des réservations n’est pas encore disponible." />} />
      <Route path="/verification" element={
        <Card>
          <CardHeader>
            <h1 className="font-heading text-xl font-medium">Navigation réussie</h1>
            <CardDescription>React Router affiche cette seconde page sans recharger le document.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <p>Le thème et le mode restent sélectionnés pendant la navigation.</p>
            <Link className={buttonVariants()} to="/">Revenir à l’accueil</Link>
          </CardContent>
        </Card>
      } />
    </Route>
  </Routes>
}

export default App
