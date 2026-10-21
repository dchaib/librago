import { useId, useState } from 'react'
import { useSearchParams } from 'react-router'
import { Dialog } from '@base-ui/react/dialog'
import { Popover } from '@base-ui/react/popover'
import { Button } from '@/components/ui/button'
import { Select, SelectContent, SelectGroup, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { cn } from '@/lib/utils'
import { applicationTimeZone, emptyFilters, filterParams, formatDueDate, parseFilters, relativeDeadline, remainingDays, selectLoans, severities, severityFor, severityLabels, type Filters, type Loan, type Option } from './loans'
import { borrowers, mockLoans, mockSynchronizations, networks } from './mock-loans'
import { useCalendarDate } from './use-calendar-date'
import { ChevronDownIcon, ClockIcon, LandmarkIcon as LibraryIcon, TriangleAlertIcon, UserRoundIcon as BorrowerIcon } from 'lucide-react'
import './loans.css'

function FilterIcon() {
  return <svg aria-hidden="true" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6"><path d="M4 4h16l-6 8v7l-4-2v-5Z" strokeLinejoin="round" /></svg>
}

function Choice({ label, all, options, value, onChange }: { label: string; all: string; options: Option[]; value: string; onChange: (value: string) => void }) {
  const id = useId()
  const allValue = '__all__'
  const items = [{ label: all, value: allValue }, ...options.map((option) => ({ label: option.name, value: option.id }))]
  return <Select items={items} value={value || allValue} onValueChange={(nextValue) => onChange(nextValue === allValue ? '' : typeof nextValue === 'string' ? nextValue : '')}>
    <div className="min-w-0 space-y-2">
      <label htmlFor={id} className="block text-sm font-medium">{label}</label>
      <SelectTrigger id={id} className="w-full bg-card data-[size=default]:h-11"><SelectValue /></SelectTrigger>
    </div>
    <SelectContent alignItemWithTrigger={false}>
      <SelectGroup>
        {items.map((item) => <SelectItem key={item.value} value={item.value}>{item.label}</SelectItem>)}
      </SelectGroup>
    </SelectContent>
  </Select>
}

function SeverityChoices({ filters, onChange }: { filters: Filters; onChange: (filters: Filters) => void }) {
  return <fieldset className="space-y-1">
    <legend className="mb-2 text-sm font-medium">Échéance</legend>
    {severities.map((severity) => <label key={severity} className="flex min-h-11 cursor-pointer items-center gap-3 rounded-md px-2 text-sm hover:bg-muted">
      <input type="checkbox" className="size-4 accent-primary focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring" checked={filters.severities.includes(severity)} onChange={(event) => onChange({ ...filters, severities: event.target.checked ? [...filters.severities, severity] : filters.severities.filter((value) => value !== severity) })} />
      {severityLabels[severity]}
    </label>)}
    <p className="pt-2 text-xs text-muted-foreground">Sans sélection, toutes les échéances sont incluses.</p>
  </fieldset>
}

function Cover({ loan }: { loan: Loan }) {
  const [failedSource, setFailedSource] = useState<string>()
  return <div className="loan-cover flex shrink-0 items-center justify-center overflow-hidden rounded-sm bg-muted text-muted-foreground">
    {loan.cover && loan.cover !== failedSource ? <img src={loan.cover} alt="" width="54" height="81" loading="lazy" className="h-full w-full object-cover" onError={() => setFailedSource(loan.cover)} /> :
      <svg aria-label="Couverture indisponible" role="img" className="size-6" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5"><path d="M12 5c-3-2-6-2-9-1v15c3-1 6-1 9 1 3-2 6-2 9-1V4c-3-1-6-1-9 1Zm0 0v15" /></svg>}
  </div>
}

function Deadline({ dueDate, today }: { dueDate: string; today: string }) {
  const days = remainingDays(dueDate, today)
  const severity = severityFor(days)
  return <div className="text-right">
    <span className={cn('loan-deadline inline-flex max-w-full items-center justify-end gap-1.5 rounded-full font-semibold',
      severity === 'overdue' ? 'bg-danger text-danger-foreground' :
      severity === 'very-close' || severity === 'close' ? 'bg-warning text-warning-foreground' : 'bg-deadline-neutral text-deadline-neutral-foreground')}>
      {severity === 'overdue' ? <TriangleAlertIcon className="size-4 shrink-0" /> : <ClockIcon className="size-4 shrink-0" />}
      <span>{relativeDeadline(days)}</span>
    </span>
    <time dateTime={dueDate} className="mt-1 block text-[11px] leading-4 text-muted-foreground md:text-xs"><span className="sr-only">Retour au plus tard le </span>{formatDueDate(dueDate)}</time>
  </div>
}

export function LoansPage({ loans = mockLoans }: { loans?: Loan[] }) {
  const today = useCalendarDate()
  const [params, setParams] = useSearchParams()
  const filters = parseFilters(params, networks, borrowers)
  const severitySummary = filters.severities.length ? `${filters.severities.length} niveau${filters.severities.length > 1 ? 'x' : ''} sélectionné${filters.severities.length > 1 ? 's' : ''}` : 'Toutes les échéances'
  const [open, setOpen] = useState(false)
  const [pending, setPending] = useState<Filters>(emptyFilters)
  const apply = (value: Filters) => setParams(filterParams(value), { replace: true })
  const visibleLoans = selectLoans(loans, filters, today)
  const networkName = (id: string) => networks.find((network) => network.id === id)?.name ?? id
  const borrowerName = (id: string) => borrowers.find((borrower) => borrower.id === id)?.name ?? id
  const badges = [
    ...(filters.network ? [{ label: `Réseau : ${networkName(filters.network)}`, remove: () => apply({ ...filters, network: '' }) }] : []),
    ...(filters.borrower ? [{ label: `Emprunteur : ${borrowerName(filters.borrower)}`, remove: () => apply({ ...filters, borrower: '' }) }] : []),
    ...filters.severities.map((severity) => ({ label: `Échéance : ${severityLabels[severity]}`, remove: () => apply({ ...filters, severities: filters.severities.filter((value) => value !== severity) }) })),
  ]
  return <section aria-labelledby="loans-heading" className="loans-page space-y-4 sm:space-y-5">
    <header className="loans-heading">
      <div className="loans-heading-copy">
        <h1 id="loans-heading" className="font-heading text-[2.5rem] leading-[1.1] font-bold tracking-tight sm:text-[3.5rem]">Prêts en cours</h1>
      </div>
      <div className="reading-illustration" aria-hidden="true" />
    </header>

    <div className="loans-filters hidden grid-cols-3 items-end gap-4 rounded-xl border p-4 md:grid">
      <Choice label="Réseau" all="Tous les réseaux" options={networks} value={filters.network} onChange={(network) => apply({ ...filters, network })} />
      <Choice label="Emprunteur" all="Toute la famille" options={borrowers} value={filters.borrower} onChange={(borrower) => apply({ ...filters, borrower })} />
      <Popover.Root>
        <div className="space-y-2"><p className="text-sm font-medium">Échéance</p>
          <Popover.Trigger className="loan-choice flex h-11 w-full items-center rounded-lg border border-input px-3 text-left text-sm" aria-label={`Filtrer par échéance : ${severitySummary}${filters.severities.length ? `, ${filters.severities.map((severity) => severityLabels[severity]).join(', ')}` : ''}`}><span>{severitySummary}</span><ChevronDownIcon aria-hidden="true" className="loan-choice-indicator size-4" /></Popover.Trigger>
        </div>
        <Popover.Portal><Popover.Positioner sideOffset={8} className="z-20"><Popover.Popup className="loan-filter-popup w-72 rounded-xl border bg-popover p-4 text-popover-foreground shadow-lg"><SeverityChoices filters={filters} onChange={apply} /></Popover.Popup></Popover.Positioner></Popover.Portal>
      </Popover.Root>
    </div>

    <div className="flex items-center justify-between gap-3">
      <p role="status" className="flex items-baseline gap-1.5 text-sm"><span className="font-heading text-2xl font-medium tabular-nums">{visibleLoans.length}</span> prêt{visibleLoans.length !== 1 ? 's' : ''}{badges.length > 0 && <span className="text-muted-foreground">sur {loans.length}</span>}</p>
      <p className="hidden text-xs text-muted-foreground md:block">Par date de retour croissante</p>
      <Dialog.Root open={open} onOpenChange={(nextOpen) => { if (nextOpen) setPending(filters); setOpen(nextOpen) }}>
        <Dialog.Trigger render={<Button className="loan-filter-action h-11 md:hidden" />}><FilterIcon />Filtres{badges.length > 0 ? ` (${badges.length})` : ''}</Dialog.Trigger>
        <Dialog.Portal>
          <Dialog.Backdrop className="fixed inset-0 z-30 bg-foreground/40" />
          <Dialog.Popup className="loan-filter-dialog fixed inset-x-0 bottom-0 z-40 max-h-[90dvh] overflow-y-auto rounded-t-2xl border bg-popover p-5 text-popover-foreground shadow-lg sm:inset-x-auto sm:bottom-auto sm:top-1/2 sm:left-1/2 sm:w-96 sm:-translate-x-1/2 sm:-translate-y-1/2 sm:rounded-xl">
            <Dialog.Title className="font-heading text-2xl">Filtrer les prêts</Dialog.Title>
            <Dialog.Description className="mt-2 text-sm text-muted-foreground">Choisissez les prêts à afficher, puis validez.</Dialog.Description>
            <div className="my-5 space-y-4">
              <Choice label="Réseau" all="Tous les réseaux" options={networks} value={pending.network} onChange={(network) => setPending({ ...pending, network })} />
              <Choice label="Emprunteur" all="Toute la famille" options={borrowers} value={pending.borrower} onChange={(borrower) => setPending({ ...pending, borrower })} />
              <SeverityChoices filters={pending} onChange={setPending} />
            </div>
            <Button variant="ghost" className="mb-3 h-11" onClick={() => setPending(emptyFilters)}>Réinitialiser les filtres</Button>
            <div className="flex flex-wrap gap-2">
              <Dialog.Close render={<Button variant="outline" className="h-11" />}>Annuler</Dialog.Close>
              <Button className="h-11 grow" onClick={() => { apply(pending); setOpen(false) }}>Afficher les prêts</Button>
            </div>
          </Dialog.Popup>
        </Dialog.Portal>
      </Dialog.Root>
    </div>
    {badges.length > 0 && <div aria-label="Filtres actifs" className="flex flex-wrap items-center gap-2">
      {badges.map((badge) => <Button key={badge.label} variant="outline" className="h-auto min-h-9 max-w-full gap-2 whitespace-normal py-1 text-left text-xs" aria-label={`Retirer le filtre ${badge.label}`} onClick={badge.remove}>{badge.label}<span aria-hidden="true">×</span></Button>)}
      <Button variant="link" className="h-9 text-xs text-foreground" onClick={() => apply(emptyFilters)}>Tout effacer</Button>
    </div>}

    {visibleLoans.length === 0 ? <div className="rounded-xl border bg-card px-5 py-12 text-center">
      <h3 className="font-heading text-2xl">{loans.length === 0 ? 'Aucun prêt en cours' : 'Aucun prêt ne correspond aux filtres'}</h3>
      <p className="mt-2 text-sm text-muted-foreground">{loans.length === 0 ? 'Tous les documents ont été rendus. À bientôt pour de nouvelles lectures !' : 'Modifiez vos filtres ou retrouvez les prêts de toute la famille.'}</p>
      {badges.length > 0 && <Button variant="outline" className="mt-5 h-11" onClick={() => apply(emptyFilters)}>Effacer les filtres</Button>}
    </div> : <>
      <ul aria-label="Prêts par date de retour croissante" className="space-y-1.5 md:hidden">
        {visibleLoans.map((loan) => <li key={loan.id} className="loan-mobile-row grid items-start gap-2 rounded-xl border bg-card px-2 py-3 text-card-foreground min-[375px]:gap-3 min-[375px]:px-3">
          <Cover loan={loan} />
          <div className="min-w-0 [overflow-wrap:anywhere]">
            <h3 className="font-heading text-[15px] leading-5 font-semibold sm:text-base sm:leading-[1.35]">{loan.title}</h3>
            {loan.author && <p className="mt-0.5 text-xs leading-4 text-muted-foreground">{loan.author}</p>}
            <div className="mt-2 flex flex-wrap gap-x-3 gap-y-1 text-xs leading-4 text-muted-foreground">
              <span className="inline-flex min-w-0 items-start gap-1"><LibraryIcon aria-hidden="true" className="mt-0.5 size-3 shrink-0" /><span><span className="sr-only">Réseau : </span>{networkName(loan.networkId)}</span></span>
              <span className="inline-flex min-w-0 items-start gap-1"><BorrowerIcon aria-hidden="true" className="mt-0.5 size-3 shrink-0" /><span><span className="sr-only">Emprunteur : </span>{borrowerName(loan.borrowerId)}</span></span>
            </div>
          </div>
          <Deadline dueDate={loan.dueDate} today={today} />
        </li>)}
      </ul>
      <div className="hidden md:block">
        <table className="loans-table w-full table-fixed text-left text-sm text-card-foreground">
          <caption className="sr-only">Prêts de toute la famille, par date de retour croissante</caption>
          <thead className="text-[11px] tracking-[0.08em] text-muted-foreground uppercase"><tr>
            <th scope="col" className="w-[43%] px-4 py-3 font-medium">Document</th>
            <th scope="col" className="w-[17%] px-3 py-3 font-medium">Réseau</th>
            <th scope="col" className="w-[14%] px-3 py-3 font-medium">Emprunteur</th>
            <th scope="col" className="w-[26%] px-4 py-3 text-right font-medium">Échéance de retour</th>
          </tr></thead>
          <tbody>{visibleLoans.map((loan) => <tr key={loan.id}>
            <th scope="row" className="px-4 py-3 font-normal"><div className="flex items-center gap-3"><Cover loan={loan} /><div className="min-w-0 [overflow-wrap:anywhere]"><p className="font-heading text-[17px] leading-6 font-semibold">{loan.title}</p>{loan.author && <p className="mt-1 text-xs text-muted-foreground">{loan.author}</p>}</div></div></th>
            <td className="px-3 py-3">{networkName(loan.networkId)}</td><td className="px-3 py-3">{borrowerName(loan.borrowerId)}</td><td className="px-4 py-3"><Deadline dueDate={loan.dueDate} today={today} /></td>
          </tr>)}</tbody>
        </table>
      </div>
    </>}

    <footer className="loans-sync space-y-3 rounded-xl border p-4 text-xs text-muted-foreground">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <h3 className="max-w-60 font-heading text-base leading-snug font-medium text-foreground">Dernière synchronisation réussie des prêts</h3>
        <ul className="flex flex-wrap gap-x-8 gap-y-3">{mockSynchronizations.map((network) => <li key={network.id} className="space-y-1"><p className="font-medium text-foreground">{network.name}</p><time dateTime={network.lastSuccess}>{new Intl.DateTimeFormat('fr-FR', { timeZone: applicationTimeZone, dateStyle: 'short', timeStyle: 'short' }).format(new Date(network.lastSuccess))}</time></li>)}</ul>
      </div>
      <p>Données de démonstration · horaires de Paris</p>
    </footer>
  </section>
}
