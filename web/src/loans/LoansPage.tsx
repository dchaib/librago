import { useState } from "react"
import { useSearchParams } from "react-router"
import { BookOpenIcon, ListFilterIcon, XIcon } from "lucide-react"

import { Button } from "@/components/ui/button"
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog"
import {
  Empty,
  EmptyContent,
  EmptyDescription,
  EmptyHeader,
  EmptyMedia,
  EmptyTitle,
} from "@/components/ui/empty"
import { LoanFilters } from "./loan-filters"
import { LoanList } from "./loan-list"
import {
  applicationTimeZone,
  emptyFilters,
  filterParams,
  parseFilters,
  selectLoans,
  severityLabels,
  type Filters,
  type Loan,
} from "./loans"
import {
  borrowers,
  mockLoans,
  mockSynchronizations,
  networks,
} from "./mock-loans"
import { useCalendarDate } from "./use-calendar-date"

export function LoansPage({ loans = mockLoans }: { loans?: Loan[] }) {
  const today = useCalendarDate()
  const [params, setParams] = useSearchParams()
  const filters = parseFilters(params, networks, borrowers)
  const [open, setOpen] = useState(false)
  const [pending, setPending] = useState<Filters>(emptyFilters)
  const apply = (value: Filters) =>
    setParams(filterParams(value), { replace: true })
  const visibleLoans = selectLoans(loans, filters, today)
  const pendingCount = selectLoans(loans, pending, today).length
  const hasPendingFilters = Boolean(
    pending.network || pending.borrower || pending.severities.length
  )
  const activeFilters = [
    ...(filters.network
      ? [
          {
            label: `Réseau : ${networks.find((network) => network.id === filters.network)?.name}`,
            remove: () => apply({ ...filters, network: "" }),
          },
        ]
      : []),
    ...(filters.borrower
      ? [
          {
            label: `Emprunteur : ${borrowers.find((borrower) => borrower.id === filters.borrower)?.name}`,
            remove: () => apply({ ...filters, borrower: "" }),
          },
        ]
      : []),
    ...filters.severities.map((severity) => ({
      label: `Échéance : ${severityLabels[severity]}`,
      remove: () =>
        apply({
          ...filters,
          severities: filters.severities.filter((value) => value !== severity),
        }),
    })),
  ]

  return (
    <section aria-labelledby="loans-heading" className="flex flex-col gap-6">
      <h1 id="loans-heading" className="text-2xl font-semibold">
        Prêts en cours
      </h1>

      <div className="hidden md:block">
        <LoanFilters value={filters} onChange={apply} />
      </div>

      <div className="flex items-center justify-between gap-3">
        <p role="status" className="text-sm">
          {visibleLoans.length} prêt{visibleLoans.length !== 1 ? "s" : ""}
          {activeFilters.length > 0 && (
            <span className="text-muted-foreground"> sur {loans.length}</span>
          )}
        </p>
        <p className="hidden text-sm text-muted-foreground md:block">
          Par date de retour croissante
        </p>
        <Dialog
          open={open}
          onOpenChange={(nextOpen) => {
            if (nextOpen) setPending(filters)
            setOpen(nextOpen)
          }}
        >
          <DialogTrigger
            render={<Button variant="outline" className="md:hidden" />}
          >
            <ListFilterIcon aria-hidden="true" data-icon="inline-start" />
            Filtres
            {activeFilters.length > 0 ? ` (${activeFilters.length})` : ""}
          </DialogTrigger>
          <DialogContent className="flex max-h-[calc(100dvh-2rem)] flex-col">
            <DialogHeader className="shrink-0">
              <div className="flex flex-wrap items-center justify-between gap-2 pr-8">
                <DialogTitle>Filtrer les prêts</DialogTitle>
                {hasPendingFilters && (
                  <Button
                    variant="link"
                    size="sm"
                    onClick={() => setPending(emptyFilters)}
                  >
                    Réinitialiser
                  </Button>
                )}
              </div>
              <DialogDescription className="sr-only">
                Choisissez les prêts à afficher, puis validez.
              </DialogDescription>
            </DialogHeader>
            <div className="-mx-4 min-h-0 overflow-y-auto px-4">
              <LoanFilters value={pending} onChange={setPending} mobile />
            </div>
            <DialogFooter className="shrink-0">
              <Button
                className="w-full"
                onClick={() => {
                  apply(pending)
                  setOpen(false)
                }}
              >
                Afficher{" "}
                {pendingCount === 1 ? "le prêt" : `les ${pendingCount} prêts`}
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      </div>

      {activeFilters.length > 0 && (
        <div
          aria-label="Filtres actifs"
          className="flex flex-wrap items-center gap-2"
        >
          {activeFilters.map((filter) => (
            <Button
              key={filter.label}
              variant="outline"
              size="sm"
              className="h-auto max-w-full text-left whitespace-normal"
              aria-label={`Retirer le filtre ${filter.label}`}
              onClick={filter.remove}
            >
              {filter.label}
              <XIcon aria-hidden="true" data-icon="inline-end" />
            </Button>
          ))}
          <Button variant="link" size="sm" onClick={() => apply(emptyFilters)}>
            Tout effacer
          </Button>
        </div>
      )}

      {visibleLoans.length ? (
        <LoanList loans={visibleLoans} today={today} />
      ) : (
        <Empty>
          <EmptyHeader>
            <EmptyMedia variant="icon">
              <BookOpenIcon aria-hidden="true" />
            </EmptyMedia>
            <EmptyTitle>
              <h2>
                {loans.length
                  ? "Aucun prêt ne correspond aux filtres"
                  : "Aucun prêt en cours"}
              </h2>
            </EmptyTitle>
            <EmptyDescription>
              {loans.length
                ? "Modifiez vos filtres ou retrouvez les prêts de toute la famille."
                : "Tous les documents ont été rendus. À bientôt pour de nouvelles lectures !"}
            </EmptyDescription>
          </EmptyHeader>
          {activeFilters.length > 0 && (
            <EmptyContent>
              <Button variant="outline" onClick={() => apply(emptyFilters)}>
                Effacer les filtres
              </Button>
            </EmptyContent>
          )}
        </Empty>
      )}

      <footer>
        <Card>
          <CardHeader>
            <CardTitle>
              <h2>Dernière synchronisation réussie des prêts</h2>
            </CardTitle>
            <CardDescription>
              Données de démonstration · horaires de Paris
            </CardDescription>
          </CardHeader>
          <CardContent>
            <dl className="grid gap-4 sm:grid-cols-2">
              {mockSynchronizations.map((network) => (
                <div key={network.id}>
                  <dt className="font-medium">{network.name}</dt>
                  <dd className="text-muted-foreground">
                    <time dateTime={network.lastSuccess}>
                      {new Intl.DateTimeFormat("fr-FR", {
                        timeZone: applicationTimeZone,
                        dateStyle: "short",
                        timeStyle: "short",
                      }).format(new Date(network.lastSuccess))}
                    </time>
                  </dd>
                </div>
              ))}
            </dl>
          </CardContent>
        </Card>
      </footer>
    </section>
  )
}
