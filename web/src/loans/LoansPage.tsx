import { useState } from "react"
import { useSearchParams } from "react-router"
import { ListFilterIcon, XIcon } from "lucide-react"

import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog"
import { LoanFilters } from "./loan-filters"
import { LoanList } from "./loan-list"
import {
  LoanCoverageAlert,
  LoanEmptyState,
  LoanReadError,
} from "./loan-data-states"
import { LoanSynchronizationStatus } from "./loan-synchronization-status"
import {
  emptyFilters,
  filterParams,
  parseFilters,
  selectLoans,
  severityLabels,
  type Filters,
} from "./loans"
import { confidence, type LoansResponse } from "./loans-api"
import { useLoans } from "./use-loans"
import { useCalendarDate } from "./use-calendar-date"

export function LoansPage() {
  const { data, error, now, isLoading, retry } = useLoans()
  if (!data)
    return (
      <section aria-labelledby="loans-heading" className="flex flex-col gap-6">
        <h1 id="loans-heading" className="text-2xl font-semibold">
          Prêts en cours
        </h1>
        {error ? (
          <LoanReadError initial isLoading={isLoading} retry={retry} />
        ) : (
          <p role="status">Chargement des prêts…</p>
        )}
      </section>
    )
  return (
    <LoadedLoans
      data={data}
      error={error}
      now={now}
      isLoading={isLoading}
      retry={retry}
    />
  )
}

function LoadedLoans({
  data,
  error,
  now,
  isLoading,
  retry,
}: {
  data: LoansResponse
  error: boolean
  now: number
  isLoading: boolean
  retry: () => void
}) {
  const { loans, networks, borrowers, synchronizations, timeZone } = data
  const today = useCalendarDate(timeZone)
  const [params, setParams] = useSearchParams()
  const filters = parseFilters(params, networks, borrowers)
  const [open, setOpen] = useState(false)
  const [pending, setPending] = useState<Filters>(emptyFilters)
  const apply = (value: Filters) =>
    setParams(filterParams(value), { replace: true })
  const visibleLoans = selectLoans(loans, filters, today)
  const selectedStates = synchronizations.filter(
    (s) => !filters.network || s.networkId === filters.network
  )
  const scope = confidence(selectedStates, now, error)
  const total = confidence(synchronizations, now, error)
  const pendingCount = selectLoans(loans, pending, today).length
  const pendingScope = confidence(
    synchronizations.filter(
      (state) => !pending.network || state.networkId === pending.network
    ),
    now,
    error
  )
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

      {error && <LoanReadError isLoading={isLoading} retry={retry} />}
      <LoanCoverageAlert
        hasConfiguredAccounts={data.hasConfiguredAccounts}
        scope={scope}
      />

      <div className="hidden md:block">
        <LoanFilters
          value={filters}
          onChange={apply}
          networks={networks}
          borrowers={borrowers}
        />
      </div>

      <div className="flex items-center justify-between gap-3">
        <p role="status" className="text-sm">
          {scope.unavailable ? "—" : visibleLoans.length} prêt
          {visibleLoans.length !== 1 ? "s" : ""}
          {!scope.unavailable &&
            !scope.reliable &&
            (visibleLoans.length === 1 ? " connu" : " connus")}
          {activeFilters.length > 0 && (
            <span className="text-muted-foreground">
              {" "}
              sur {total.unavailable ? "—" : loans.length}
              {!total.unavailable &&
                !total.reliable &&
                (loans.length === 1 ? " connu" : " connus")}{" "}
              au total
            </span>
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
                Choisissez vos filtres, puis appuyez sur Afficher.
              </DialogDescription>
            </DialogHeader>
            <div className="-mx-4 min-h-0 overflow-y-auto px-4">
              <LoanFilters
                value={pending}
                onChange={setPending}
                mobile
                networks={networks}
                borrowers={borrowers}
              />
            </div>
            <DialogFooter className="shrink-0">
              <Button
                className="w-full"
                onClick={() => {
                  apply(pending)
                  setOpen(false)
                }}
              >
                {filterActionLabel(pendingCount, pendingScope)}
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
            Effacer les filtres
          </Button>
        </div>
      )}

      {visibleLoans.length ? (
        <LoanList
          loans={visibleLoans}
          today={today}
          networks={networks}
          borrowers={borrowers}
        />
      ) : (
        <LoanEmptyState
          hasConfiguredAccounts={data.hasConfiguredAccounts}
          unavailable={scope.unavailable}
          reliable={scope.reliable}
          hasFilters={activeFilters.length > 0}
          clearFilters={() => apply(emptyFilters)}
        />
      )}

      <LoanSynchronizationStatus
        synchronizations={synchronizations}
        networks={networks}
        timeZone={timeZone}
        now={now}
      />
    </section>
  )
}

function filterActionLabel(
  count: number,
  scope: ReturnType<typeof confidence>
) {
  if (scope.unavailable) return "Afficher les prêts connus"
  const loans = count === 1 ? "le prêt" : `les ${count} prêts`
  const qualifier = scope.reliable ? "" : count === 1 ? " connu" : " connus"
  return `Afficher ${loans}${qualifier}`
}
