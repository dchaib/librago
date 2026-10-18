import { useId } from 'react'

/** Small, original decorative artwork; its inks are theme roles, never statuses. */
export function ReadingIllustration() {
  const id = useId()
  return <svg className="reading-illustration" viewBox="0 0 300 160" aria-hidden="true" focusable="false">
    <defs>
      <linearGradient id={`${id}-ink`} x2="0.8" y2="1">
        <stop stopColor="var(--reading-accent)" />
        <stop offset="1" stopColor="var(--illustration-leaf)" />
      </linearGradient>
      <linearGradient id={`${id}-paper`} x2="0" y2="1">
        <stop stopColor="var(--illustration-paper)" />
        <stop offset="1" stopColor="var(--illustration-paper)" stopOpacity=".8" />
      </linearGradient>
    </defs>
    <circle cx="168" cy="97" r="77" fill="var(--illustration-leaf)" opacity=".12" />
    <path className="reading-moon" d="M83 18a15 15 0 1 0 14 25A17 17 0 0 1 83 18Z" fill="var(--primary)" opacity=".85" />
    <g fill="var(--illustration-leaf)" stroke="var(--illustration-leaf)" strokeWidth="1">
      <path d="M80 154Q72 111 51 61M67 116Q49 101 28 100M74 133Q93 108 100 86M243 153Q251 103 271 44M251 115Q274 104 287 84" fill="none" />
      <path d="M53 69Q33 63 42 37Q60 51 53 69ZM63 92Q39 89 35 70Q61 74 63 92ZM69 114Q52 117 42 98Q66 96 69 114ZM77 143Q49 140 39 122Q65 121 77 143ZM69 103Q66 80 83 68Q87 90 69 103ZM77 128Q77 106 98 101Q96 119 77 128ZM56 113Q33 118 21 101Q42 96 56 113Z" />
      <path d="M267 57Q253 39 266 17Q282 39 267 57ZM258 80Q255 57 239 50Q234 70 258 80ZM255 96Q263 71 284 64Q284 85 255 96ZM249 119Q227 106 227 87Q250 94 249 119ZM247 136Q265 111 284 116Q280 134 247 136Z" />
    </g>
    <g fill="var(--reading-accent)">
      <path d="M111 150Q89 132 92 115Q114 119 111 150ZM100 147Q75 154 65 139Q89 128 100 147ZM237 152Q219 140 218 124Q239 130 237 152ZM260 108Q273 88 292 97Q284 113 260 108Z" />
    </g>
    <g stroke="var(--reading-accent)" strokeWidth="1.2">
      <rect x="107" y="132" width="139" height="22" rx="7" fill={`url(#${id}-ink)`} />
      <path d="M115 135h103q-4 8 0 16H115q-7-8 0-16Z" fill={`url(#${id}-paper)`} />
      <rect x="119" y="109" width="119" height="22" rx="7" fill={`url(#${id}-ink)`} />
      <path d="M154 112h75q7 8 0 16h-75q4-8 0-16Z" fill={`url(#${id}-paper)`} />
      <rect x="101" y="86" width="123" height="22" rx="7" fill={`url(#${id}-ink)`} />
      <path d="M109 89h88q-4 8 0 16h-88q-7-8 0-16Z" fill={`url(#${id}-paper)`} />
      <rect x="139" y="65" width="91" height="20" rx="7" fill={`url(#${id}-ink)`} />
      <path d="M165 68h56q7 7 0 14h-56q4-7 0-14Z" fill={`url(#${id}-paper)`} />
    </g>
    <g stroke="var(--reading-accent)" opacity=".45" strokeWidth=".7">
      <path d="M116 140h98m-98 5h98m-57-28h69m-69 5h69m-116-28h84m-84 5h84m-24-26h49m-49 4h49" />
    </g>
    <path d="M22 155h265" stroke="var(--illustration-leaf)" opacity=".3" />
  </svg>
}
