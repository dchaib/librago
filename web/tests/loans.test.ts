import assert from "node:assert/strict"
import { test } from "node:test"
import {
  addDays,
  calendarDate,
  emptyFilters,
  filterParams,
  parseFilters,
  relativeDeadline,
  remainingDays,
  selectLoans,
  severityFor,
  type Loan,
} from "../src/loans/loans.ts"
import { borrowers, createMockLoans, networks } from "./fixtures/mock-loans.ts"

test("severity boundaries and inclusive due dates", () => {
  const cases = [
    [-1, "overdue"],
    [0, "very-close"],
    [2, "very-close"],
    [3, "close"],
    [6, "close"],
    [7, "watch"],
    [13, "watch"],
    [14, "normal"],
  ] as const
  for (const [days, expected] of cases)
    assert.equal(severityFor(days), expected)
  assert.equal(relativeDeadline(-1), "En retard de 1 jour")
  assert.equal(relativeDeadline(-3), "En retard de 3 jours")
  assert.equal(relativeDeadline(0), "Aujourd’hui")
  assert.equal(relativeDeadline(1), "Demain")
  assert.equal(relativeDeadline(2), "Dans 2 jours")
})

test("configured calendar dates across midnight and DST, independent of host zone", () => {
  assert.equal(calendarDate(new Date("2026-10-14T22:30:00Z")), "2026-10-15")
  assert.equal(
    calendarDate(new Date("2026-10-14T22:30:00Z"), "America/New_York"),
    "2026-10-14"
  )
  for (const [start, end] of [
    ["2026-03-28T23:00:00Z", "2026-03-29T22:00:00Z"],
    ["2026-10-24T22:00:00Z", "2026-10-25T23:00:00Z"],
  ]) {
    assert.equal(
      remainingDays(calendarDate(new Date(end)), calendarDate(new Date(start))),
      1
    )
  }
  assert.equal(addDays("2028-02-28", 1), "2028-02-29")
  assert.equal(addDays("2026-12-31", 1), "2027-01-01")
  assert.equal(
    remainingDays("2026-10-15", calendarDate(new Date("2026-10-15T21:59:59Z"))),
    0
  )
  assert.equal(
    remainingDays("2026-10-15", calendarDate(new Date("2026-10-15T22:00:00Z"))),
    -1
  )
})

const today = "2026-10-14"
const loans = createMockLoans(today)
test("mock data covers every severity, and sorting does not mutate the input", () => {
  assert.equal(loans.length, 24)
  assert.equal(
    new Set(
      loans.map((loan) => severityFor(remainingDays(loan.dueDate, today)))
    ).size,
    5
  )
  const reversed = [...loans].reverse()
  const original = [...reversed]
  const sorted = selectLoans(reversed, emptyFilters, today)
  assert.deepEqual(reversed, original)
  assert.ok(
    sorted.every(
      (loan, index) => index === 0 || sorted[index - 1].dueDate <= loan.dueDate
    )
  )
  assert.equal(sorted[0].dueDate, addDays(today, -7))
})

test("dimensions combine with AND; exact severities combine with OR", () => {
  const result = selectLoans(
    loans,
    {
      network: "rivages",
      borrower: "camille",
      severities: ["very-close", "close"],
    },
    today
  )
  assert.deepEqual(
    result.map((loan) => loan.id),
    ["demo-7"]
  )
  for (const severity of ["very-close", "close"] as const) {
    const exact = selectLoans(
      loans,
      { ...emptyFilters, severities: [severity] },
      today
    )
    assert.ok(exact.length > 0)
    assert.ok(
      exact.every(
        (loan) => severityFor(remainingDays(loan.dueDate, today)) === severity
      )
    )
  }
  assert.equal(selectLoans(loans, emptyFilters, today).length, loans.length)
  assert.equal(
    selectLoans(loans, { ...emptyFilters, network: "rivages" }, today).length,
    12
  )
  assert.equal(
    selectLoans(loans, { ...emptyFilters, borrower: "lou" }, today).length,
    8
  )
  assert.deepEqual(selectLoans([], emptyFilters, today), [])
  assert.deepEqual(
    selectLoans(
      loans,
      { network: "rivages", borrower: "camille", severities: ["close"] },
      today
    ),
    []
  )
})

test("same unchanged loan ages from imminent to overdue at midnight", () => {
  const loan: Loan = {
    id: "test",
    title: "Test",
    networkId: "rivages",
    borrowerId: "lou",
    dueDate: today,
  }
  assert.equal(
    selectLoans([loan], { ...emptyFilters, severities: ["overdue"] }, today)
      .length,
    0
  )
  assert.equal(
    selectLoans(
      [loan],
      { ...emptyFilters, severities: ["overdue"] },
      addDays(today, 1)
    ).length,
    1
  )
})

test("URL filters round-trip, deduplicate, ignore invalid values and clear", () => {
  const filters = {
    network: "rivages",
    borrower: "lou",
    severities: ["overdue", "close"] as const,
  }
  const mutable = { ...filters, severities: [...filters.severities] }
  assert.deepEqual(
    parseFilters(filterParams(mutable), networks, borrowers),
    mutable
  )
  assert.equal(filterParams(emptyFilters).toString(), "")
  assert.deepEqual(
    parseFilters(new URLSearchParams(), networks, borrowers),
    emptyFilters
  )
  assert.deepEqual(
    parseFilters(
      new URLSearchParams(
        "network=invalid&borrower=invalid&severity=close&severity=close&severity=unknown"
      ),
      networks,
      borrowers
    ),
    { ...emptyFilters, severities: ["close"] }
  )
  assert.deepEqual(
    parseFilters(new URLSearchParams("borrower=lou"), networks, borrowers),
    { ...emptyFilters, borrower: "lou" }
  )
})
