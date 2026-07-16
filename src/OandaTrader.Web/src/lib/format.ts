export function money(value: number | null | undefined, currency = ''): string {
  if (value == null) return '—'
  const formatted = value.toLocaleString(undefined, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })
  return currency ? `${formatted} ${currency}` : formatted
}

export function signedMoney(value: number | null | undefined, currency = ''): string {
  if (value == null) return '—'
  const sign = value > 0 ? '+' : ''
  return `${sign}${money(value, currency)}`
}

export function percent(value: number | null | undefined, digits = 1): string {
  if (value == null) return '—'
  return `${value.toFixed(digits)}%`
}

/** Takes a 0..1 probability. */
export function probabilityPercent(value: number | null | undefined, digits = 1): string {
  if (value == null) return '—'
  return `${(value * 100).toFixed(digits)}%`
}

export function price(value: number | null | undefined): string {
  if (value == null) return '—'
  return value.toLocaleString(undefined, { minimumFractionDigits: 3, maximumFractionDigits: 5 })
}

export function dateTime(iso: string | null | undefined): string {
  if (!iso) return '—'
  const d = new Date(iso.endsWith('Z') || iso.includes('+') ? iso : `${iso}Z`)
  if (Number.isNaN(d.getTime())) return '—'
  return d.toLocaleString(undefined, {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

export function timeOnly(iso: string | null | undefined): string {
  if (!iso) return '—'
  const d = new Date(iso.endsWith('Z') || iso.includes('+') ? iso : `${iso}Z`)
  if (Number.isNaN(d.getTime())) return '—'
  return d.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit', second: '2-digit' })
}

export function pnlClass(value: number | null | undefined): string {
  if (value == null || value === 0) return ''
  return value > 0 ? 'pos' : 'neg'
}
