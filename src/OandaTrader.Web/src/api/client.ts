export type Granularity = 'M1' | 'M5' | 'M15' | 'M30' | 'H1' | 'H4' | 'D'
export const GRANULARITIES: Granularity[] = ['M1', 'M5', 'M15', 'M30', 'H1', 'H4', 'D']

export interface Settings {
  id: number
  riskPercentPerTrade: number
  granularity: Granularity
  maxDailyLossPercent: number
  maxConcurrentPositions: number
  maxTradesPerDay: number
  mlConfidenceThreshold: number
  retrainAfterTradeCount: number
  engineEnabled: boolean
  pausedReason: string | null
  pausedAtUtc: string | null
}

export interface InstrumentSetting {
  id: number
  instrument: string
  enabled: boolean
}

export interface Trade {
  id: number
  oandaTradeId: string | null
  instrument: string
  direction: 'Long' | 'Short'
  entryPrice: number
  entryTimeUtc: string
  stopLoss: number
  takeProfit: number
  units: number
  exitPrice: number | null
  exitTimeUtc: string | null
  pnL: number | null
  outcome: 'Open' | 'Win' | 'Loss' | 'Breakeven'
  strategySource: 'Backtest' | 'Live'
  featuresJson: string
  reasoningText: string
  mlConfidence: number | null
}

export interface AccountSummary {
  account: {
    id: string
    alias: string | null
    currency: string
    balance: number
    nav: number
    unrealizedPl: number
    marginUsed: number
    marginAvailable: number
    openTradeCount: number
    openPositionCount: number
  }
}

export interface EngineStatus {
  engineEnabled: boolean
  pausedReason: string | null
  pausedAtUtc: string | null
}

export interface ModelVersion {
  id: number
  trainedAtUtc: string
  trainingSampleCount: number
  metricsJson: string
  isActive: boolean
  modelFilePath: string
}

export interface AnalyticsSummary {
  source: string
  tradeCount: number
  wins: number
  losses: number
  winRatePercent: number
  totalPnL: number
  equityCurve: { index: number; time: string; cumulativePnL: number }[]
  perInstrument: {
    instrument: string
    trades: number
    wins: number
    winRatePercent: number
    totalPnL: number
  }[]
}

export interface BacktestRun {
  id: number
  runAtUtc: string
  instrument: string
  granularity: Granularity
  startDate: string
  endDate: string
  resultSummaryJson: string
}

export interface CircuitBreakerEvent {
  id: number
  timestampUtc: string
  reason: string
  triggeredLimit: string
  resumedAtUtc: string | null
}

/** Surfaces the API's `{ error }` body as the thrown message so callers can show it verbatim. */
async function request<T>(url: string, init?: RequestInit): Promise<T> {
  const res = await fetch(url, {
    ...init,
    headers: { 'Content-Type': 'application/json', ...(init?.headers ?? {}) },
  })

  const text = await res.text()
  const body = text ? JSON.parse(text) : null

  if (!res.ok) {
    throw new Error(body?.error ?? `Request failed (${res.status})`)
  }
  return body as T
}

export const api = {
  health: () => request<{ status: string; timestampUtc: string }>('/api/health'),

  accountSummary: () => request<AccountSummary>('/api/account/summary'),

  engineStatus: () => request<EngineStatus>('/api/engine/status'),
  engineStart: () => request<{ engineEnabled: boolean }>('/api/engine/start', { method: 'POST' }),
  engineStop: () => request<{ engineEnabled: boolean }>('/api/engine/stop', { method: 'POST' }),
  engineResume: () => request<{ pausedReason: string | null }>('/api/engine/resume', { method: 'POST' }),

  getSettings: () => request<{ settings: Settings; instruments: InstrumentSetting[] }>('/api/settings'),
  updateSettings: (body: {
    riskPercentPerTrade: number
    granularity: Granularity
    maxDailyLossPercent: number
    maxConcurrentPositions: number
    maxTradesPerDay: number
    mlConfidenceThreshold: number
    retrainAfterTradeCount: number
  }) => request<Settings>('/api/settings', { method: 'PUT', body: JSON.stringify(body) }),
  updateInstrument: (instrument: string, enabled: boolean) =>
    request<InstrumentSetting>(`/api/settings/instruments/${instrument}`, {
      method: 'PUT',
      body: JSON.stringify({ enabled }),
    }),

  trades: (strategySource?: string, take = 100) =>
    request<Trade[]>(
      `/api/trades?take=${take}${strategySource ? `&strategySource=${strategySource}` : ''}`,
    ),
  openTrades: () => request<Trade[]>('/api/trades/open'),

  analytics: (strategySource = 'Live') =>
    request<AnalyticsSummary>(`/api/analytics/summary?strategySource=${strategySource}`),
  circuitBreakerEvents: () => request<CircuitBreakerEvent[]>('/api/analytics/circuit-breaker-events'),

  modelStatus: () => request<{ ready: boolean }>('/api/model/status'),
  modelVersions: () => request<ModelVersion[]>('/api/model/versions'),
  trainModel: () =>
    request<{ id: number; trainedAtUtc: string; trainingSampleCount: number; promoted: boolean; metrics: string }>(
      '/api/model/train',
      { method: 'POST' },
    ),

  // Kicks the backtest off in the background; the run's own progress/completion streams
  // over SignalR as BacktestProgress events keyed by the returned jobId.
  runBacktest: (instrument: string, months: number) =>
    request<{ jobId: string }>('/api/backtest/run', {
      method: 'POST',
      body: JSON.stringify({ instrument, months }),
    }),

  backtestRuns: () => request<BacktestRun[]>('/api/backtest/runs'),
}
