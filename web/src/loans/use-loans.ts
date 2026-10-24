import { useEffect, useRef, useState } from "react"
import { fetchLoans, type LoansResponse } from "./loans-api"

export function useLoans() {
  const [data, setData] = useState<LoansResponse>()
  const [error, setError] = useState(false)
  const [now, setNow] = useState(Date.now)
  const [isLoading, setIsLoading] = useState(false)
  const retryRef = useRef(() => {})
  useEffect(() => {
    let controller: AbortController | undefined
    let disposed = false
    let lastReadAt = -Infinity
    async function load(force = false) {
      const currentTime = Date.now()
      setNow(currentTime)
      if (
        document.visibilityState === "hidden" ||
        controller ||
        (!force && currentTime - lastReadAt < 5 * 60_000)
      )
        return
      lastReadAt = currentTime
      setIsLoading(true)
      controller = new AbortController()
      const request = controller
      const timeout = setTimeout(() => request.abort(), 15_000)
      try {
        const response = await fetchLoans(controller.signal)
        if (!disposed) {
          setData(response)
          setError(false)
        }
      } catch {
        if (!disposed) setError(true)
      } finally {
        clearTimeout(timeout)
        controller = undefined
        if (!disposed) setIsLoading(false)
      }
    }
    void load()
    retryRef.current = () => void load(true)
    // Age confidence locally without making periodic API requests.
    const timer = setInterval(() => setNow(Date.now()), 60_000)
    const visible = () => void load()
    window.addEventListener("focus", visible)
    document.addEventListener("visibilitychange", visible)
    return () => {
      disposed = true
      retryRef.current = () => {}
      controller?.abort()
      clearInterval(timer)
      window.removeEventListener("focus", visible)
      document.removeEventListener("visibilitychange", visible)
    }
  }, [])
  return { data, error, now, isLoading, retry: () => retryRef.current() }
}
