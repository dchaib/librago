import assert from "node:assert/strict"
import { test } from "node:test"
import {
  confidence,
  decodeLoans,
  fetchLoans,
  isCurrent,
  type LoansResponse,
} from "../src/loans/loans-api.ts"

const at = "2026-10-20T10:00:00Z"
function response(): LoansResponse {
  return {
    loans: [
      {
        id: "loan",
        title: "Synthetic title",
        author: null,
        networkId: "network",
        borrowerId: "reader",
        dueDate: "2026-10-25",
      },
    ],
    networks: [{ id: "network", name: "Synthetic network" }],
    borrowers: [{ id: "reader", name: "Reader" }],
    synchronizations: [
      {
        networkId: "network",
        lastAttemptAt: at,
        lastCompleteSuccessAt: at,
        result: "success",
        coverage: "complete",
        hasCurrentAccountCoverage: true,
        isStale: false,
      },
    ],
    hasConfiguredAccounts: true,
    timeZone: "Europe/Paris",
  }
}

test("decodes nullable fields and rejects invalid transport data", () => {
  assert.deepEqual(decodeLoans(response()), response())
  for (const invalid of [
    null,
    {},
    { ...response(), timeZone: "Invalid/Zone" },
    {
      ...response(),
      loans: [{ ...response().loans[0], dueDate: "2026-02-30" }],
    },
    {
      ...response(),
      synchronizations: [
        { ...response().synchronizations[0], coverage: "unknown" },
      ],
    },
    {
      ...response(),
      synchronizations: [
        { ...response().synchronizations[0], lastAttemptAt: "yesterday" },
      ],
    },
  ])
    assert.throws(() => decodeLoans(invalid))
})

test("coverage and freshness remain independent and age without fetching", () => {
  const state = response().synchronizations[0]
  const now = Date.parse(at)
  assert.equal(isCurrent(state, now + 86_400_000), true)
  assert.equal(isCurrent(state, now + 86_400_001), false)
  assert.deepEqual(confidence([{ ...state, result: "failed" }], now), {
    unavailable: false,
    complete: true,
    reliable: false,
  })
  assert.equal(
    confidence([state, { ...state, coverage: "unavailable" }], now).complete,
    false
  )
  assert.equal(
    confidence([{ ...state, coverage: "unavailable" }], now).unavailable,
    true
  )
  assert.equal(
    isCurrent({ ...state, hasCurrentAccountCoverage: false }, now),
    false
  )
  assert.equal(confidence([], now).reliable, false)
})

test("a failed read qualifies both selected and total confidence without losing coverage", () => {
  const first = response().synchronizations[0]
  const second = { ...first, networkId: "other-network" }
  const now = Date.parse(at)
  for (const states of [[first], [first, second]]) {
    assert.equal(confidence(states, now).reliable, true)
    assert.deepEqual(confidence(states, now, true), {
      unavailable: false,
      complete: true,
      reliable: false,
    })
    assert.equal(confidence(states, now, false).reliable, true)
  }
})

test("HTTP client checks failures, validates JSON and forwards cancellation", async (t) => {
  const signal = new AbortController().signal
  t.mock.method(
    globalThis,
    "fetch",
    async (url: string, options: RequestInit) => {
      assert.equal(url, "/api/loans")
      assert.equal(options.signal, signal)
      assert.equal(options.cache, "no-store")
      return Response.json(response())
    }
  )
  assert.deepEqual(await fetchLoans(signal), response())
  t.mock.method(globalThis, "fetch", async () =>
    Response.json({}, { status: 500 })
  )
  await assert.rejects(fetchLoans(signal), /500/)
  t.mock.method(globalThis, "fetch", async () => new Response("<html>"))
  await assert.rejects(fetchLoans(signal))
  t.mock.method(globalThis, "fetch", async () => {
    throw new DOMException("Aborted", "AbortError")
  })
  await assert.rejects(fetchLoans(signal), { name: "AbortError" })
})
