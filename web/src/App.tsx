import { BrowserRouter, Route, Routes } from "react-router"

import { AppShell } from "@/components/AppShell"
import { LoansPage } from "@/loans/LoansPage"

function PlaceholderPage({ title }: { title: string }) {
  return (
    <div className="flex flex-col gap-2">
      <h1 className="text-2xl font-semibold">{title}</h1>
      <p className="text-sm text-muted-foreground">
        Cette page sera complétée prochainement.
      </p>
    </div>
  )
}

export function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route element={<AppShell />}>
          <Route index element={<PlaceholderPage title="Accueil" />} />
          <Route path="loans" element={<LoansPage />} />
          <Route
            path="reservations"
            element={<PlaceholderPage title="Réservations" />}
          />
        </Route>
      </Routes>
    </BrowserRouter>
  )
}

export default App
