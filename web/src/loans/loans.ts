export const applicationTimeZone = "Europe/Paris"

export const severities = [
  "overdue",
  "very-close",
  "close",
  "watch",
  "normal",
] as const
export type Severity = (typeof severities)[number]
export const severityLabels: Record<Severity, string> = {
  overdue: "En retard",
  "very-close": "Très proche · 0–2 jours",
  close: "Proche · 3–6 jours",
  watch: "À surveiller · 7–13 jours",
  normal: "Plus tard · 14 jours et plus",
}
export type Option = { id: string; name: string }
export type Loan = {
  id: string
  title: string
  author?: string
  cover?: string
  networkId: string
  borrowerId: string
  dueDate: string
}
export type Filters = {
  network: string
  borrower: string
  severities: Severity[]
}
export const emptyFilters: Filters = {
  network: "",
  borrower: "",
  severities: [],
}

// ISO calendar dates are converted to ordinal days, never elapsed local hours.
export function calendarDate(
  now: Date,
  timeZone = applicationTimeZone
): string {
  const parts = new Intl.DateTimeFormat("en", {
    timeZone,
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).formatToParts(now)
  const part = (type: string) =>
    parts.find((value) => value.type === type)!.value
  return `${part("year")}-${part("month")}-${part("day")}`
}
export function addDays(date: string, days: number): string {
  return new Date(Date.parse(`${date}T00:00:00Z`) + days * 86400000)
    .toISOString()
    .slice(0, 10)
}
export function remainingDays(dueDate: string, today: string): number {
  return (
    (Date.parse(`${dueDate}T00:00:00Z`) - Date.parse(`${today}T00:00:00Z`)) /
    86400000
  )
}
export function severityFor(days: number): Severity {
  if (days < 0) return "overdue"
  if (days <= 2) return "very-close"
  if (days <= 6) return "close"
  if (days <= 13) return "watch"
  return "normal"
}
export function relativeDeadline(days: number): string {
  if (days < 0) return `En retard de ${-days} jour${days < -1 ? "s" : ""}`
  if (days === 0) return "Aujourd’hui"
  if (days === 1) return "Demain"
  return `Dans ${days} jours`
}
export function formatDueDate(date: string): string {
  return new Intl.DateTimeFormat("fr-FR", {
    day: "numeric",
    month: "short",
    year: "numeric",
    timeZone: "UTC",
  }).format(new Date(`${date}T00:00:00Z`))
}
export function selectLoans(
  loans: Loan[],
  filters: Filters,
  today: string
): Loan[] {
  return loans
    .filter(
      (loan) =>
        (!filters.network || loan.networkId === filters.network) &&
        (!filters.borrower || loan.borrowerId === filters.borrower) &&
        (!filters.severities.length ||
          filters.severities.includes(
            severityFor(remainingDays(loan.dueDate, today))
          ))
    )
    .sort(
      (a, b) => a.dueDate.localeCompare(b.dueDate) || a.id.localeCompare(b.id)
    )
}
export function parseFilters(
  params: URLSearchParams,
  networks: Option[],
  borrowers: Option[]
): Filters {
  return {
    network:
      networks.find((option) => option.id === params.get("network"))?.id ?? "",
    borrower:
      borrowers.find((option) => option.id === params.get("borrower"))?.id ??
      "",
    severities: severities.filter((severity) =>
      params.getAll("severity").includes(severity)
    ),
  }
}
export function filterParams(filters: Filters): URLSearchParams {
  const params = new URLSearchParams()
  if (filters.network) params.set("network", filters.network)
  if (filters.borrower) params.set("borrower", filters.borrower)
  severities
    .filter((severity) => filters.severities.includes(severity))
    .forEach((severity) => params.append("severity", severity))
  return params
}
