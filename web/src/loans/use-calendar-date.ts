import { useEffect, useState } from 'react'
import { applicationTimeZone, calendarDate } from './loans'

export function useCalendarDate(timeZone = applicationTimeZone) {
  const [today, setToday] = useState(() => calendarDate(new Date(), timeZone))
  useEffect(() => {
    // Schedule at the next date boundary in the configured zone (including DST).
    let timer: ReturnType<typeof setTimeout>
    function update() {
      clearTimeout(timer)
      const now = Date.now()
      const date = calendarDate(new Date(now), timeZone)
      setToday(date)
      let low = now
      let high = now + 27 * 60 * 60 * 1000
      while (high - low > 500) {
        const middle = Math.floor((low + high) / 2)
        if (calendarDate(new Date(middle), timeZone) === date) low = middle
        else high = middle
      }
      timer = setTimeout(update, high - now)
    }
    update()
    window.addEventListener('focus', update)
    document.addEventListener('visibilitychange', update)
    return () => {
      clearTimeout(timer)
      window.removeEventListener('focus', update)
      document.removeEventListener('visibilitychange', update)
    }
  }, [timeZone])
  return today
}
