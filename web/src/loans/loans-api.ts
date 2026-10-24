import type { Loan, Option } from "./loans.ts"

export type Synchronization = {
  networkId: string
  lastAttemptAt: string | null
  lastCompleteSuccessAt: string | null
  result: "success" | "partial" | "failed" | null
  coverage: "complete" | "partial" | "unavailable"
  hasCurrentAccountCoverage: boolean
  isStale: boolean
}
export type LoansResponse = {
  loans: Loan[]
  networks: Option[]
  borrowers: Option[]
  synchronizations: Synchronization[]
  hasConfiguredAccounts: boolean
  timeZone: string
}

const record = (value: unknown): value is Record<string, unknown> =>
  typeof value === "object" && value !== null && !Array.isArray(value)
const text = (value: unknown): value is string => typeof value === "string"
const nullableText = (value: unknown) => value === null || text(value)
const timestamp = (value: unknown) =>
  value === null || (text(value) && Number.isFinite(Date.parse(value)))
const option = (value: unknown) =>
  record(value) && text(value.id) && text(value.name)

// Check the transport boundary before data can be used by dates and filters.
export function decodeLoans(value: unknown): LoansResponse {
  if (
    !record(value) ||
    !Array.isArray(value.loans) ||
    !value.loans.every(
      (loan: unknown) =>
        record(loan) &&
        text(loan.id) &&
        text(loan.title) &&
        nullableText(loan.author) &&
        text(loan.networkId) &&
        text(loan.borrowerId) &&
        text(loan.dueDate) &&
        /^\d{4}-\d{2}-\d{2}$/.test(loan.dueDate) &&
        Number.isFinite(Date.parse(loan.dueDate)) &&
        new Date(loan.dueDate).toISOString().slice(0, 10) === loan.dueDate
    ) ||
    !Array.isArray(value.networks) ||
    !value.networks.every(option) ||
    !Array.isArray(value.borrowers) ||
    !value.borrowers.every(option) ||
    !Array.isArray(value.synchronizations) ||
    !value.synchronizations.every(
      (state: unknown) =>
        record(state) &&
        text(state.networkId) &&
        timestamp(state.lastAttemptAt) &&
        timestamp(state.lastCompleteSuccessAt) &&
        [null, "success", "partial", "failed"].includes(
          state.result as string | null
        ) &&
        ["complete", "partial", "unavailable"].includes(
          state.coverage as string
        ) &&
        typeof state.hasCurrentAccountCoverage === "boolean" &&
        typeof state.isStale === "boolean"
    ) ||
    typeof value.hasConfiguredAccounts !== "boolean" ||
    !text(value.timeZone)
  )
    throw new Error("Invalid loans response")
  // Intl throws for unsupported time zones.
  new Intl.DateTimeFormat("fr-FR", { timeZone: value.timeZone }).format()
  return value as LoansResponse
}

export async function fetchLoans(signal: AbortSignal): Promise<LoansResponse> {
  const response = await fetch("/api/loans", { signal, cache: "no-store" })
  if (!response.ok) throw new Error(`Loans request failed (${response.status})`)
  return decodeLoans(await response.json())
}

export function isCurrent(state: Synchronization, now: number): boolean {
  return (
    state.coverage === "complete" &&
    state.hasCurrentAccountCoverage &&
    state.result === "success" &&
    !state.isStale &&
    state.lastCompleteSuccessAt !== null &&
    now - Date.parse(state.lastCompleteSuccessAt) <= 24 * 60 * 60 * 1000
  )
}

export function confidence(
  states: Synchronization[],
  now: number,
  readFailed = false
) {
  return {
    unavailable:
      states.length > 0 && states.every((s) => s.coverage === "unavailable"),
    complete:
      states.length > 0 && states.every((s) => s.coverage === "complete"),
    reliable:
      !readFailed &&
      states.length > 0 &&
      states.every((s) => isCurrent(s, now)),
  }
}
