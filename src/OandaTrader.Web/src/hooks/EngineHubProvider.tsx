import { createContext, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from 'react'
import * as signalR from '@microsoft/signalr'

export interface PriceUpdate {
  instrument: string
  bid: number
  ask: number
  timeUtc: string
}

export interface EngineStatusUpdate {
  engineEnabled: boolean
  pausedReason: string | null
  pausedAtUtc: string | null
}

export interface AccountUpdate {
  balance: number
  nav: number
  unrealizedPl: number
  marginUsed: number
  marginAvailable: number
  openTradeCount: number
}

export interface TradeEvent {
  type: string
  tradeId: number
  instrument: string
  direction: string
  entryPrice: number
  exitPrice: number | null
  pnL: number | null
  outcome: string
  mlConfidence: number | null
  reasoningText: string
}

export interface EngineLogEntry {
  timestampUtc: string
  level: string
  message: string
}

export interface BacktestProgressUpdate {
  jobId: string
  instrument: string
  stage: 'Fetching' | 'Simulating' | 'Completed' | 'Failed'
  percent: number
  message: string | null
}

interface EngineHubState {
  connected: boolean
  prices: Record<string, PriceUpdate>
  engineStatus: EngineStatusUpdate | null
  account: AccountUpdate | null
  logs: EngineLogEntry[]
  /** Bumped on every trade event so pages can refetch their trade/analytics queries. */
  tradeEventCount: number
  lastTradeEvent: TradeEvent | null
  /** Latest progress event per backtest jobId; entries are removed a few seconds after
   * Completed/Failed so a page can show the final state briefly before it clears. */
  backtestProgress: Record<string, BacktestProgressUpdate>
}

const EngineHubContext = createContext<EngineHubState>({
  connected: false,
  prices: {},
  engineStatus: null,
  account: null,
  logs: [],
  tradeEventCount: 0,
  lastTradeEvent: null,
  backtestProgress: {},
})

const MAX_LOGS = 100

export function EngineHubProvider({ children }: { children: ReactNode }) {
  const [connected, setConnected] = useState(false)
  const [prices, setPrices] = useState<Record<string, PriceUpdate>>({})
  const [engineStatus, setEngineStatus] = useState<EngineStatusUpdate | null>(null)
  const [account, setAccount] = useState<AccountUpdate | null>(null)
  const [logs, setLogs] = useState<EngineLogEntry[]>([])
  const [tradeEventCount, setTradeEventCount] = useState(0)
  const [lastTradeEvent, setLastTradeEvent] = useState<TradeEvent | null>(null)
  const [backtestProgress, setBacktestProgress] = useState<Record<string, BacktestProgressUpdate>>({})

  // StrictMode double-invokes effects in dev; guard so we don't open two connections.
  const connectionRef = useRef<signalR.HubConnection | null>(null)

  useEffect(() => {
    if (connectionRef.current) return

    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/engine')
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    connectionRef.current = connection

    connection.on('Price', (u: PriceUpdate) =>
      setPrices((prev) => ({ ...prev, [u.instrument]: u })),
    )
    connection.on('EngineStatus', (u: EngineStatusUpdate) => setEngineStatus(u))
    connection.on('Account', (u: AccountUpdate) => setAccount(u))
    connection.on('Trade', (t: TradeEvent) => {
      setLastTradeEvent(t)
      setTradeEventCount((n) => n + 1)
    })
    connection.on('Log', (entry: EngineLogEntry) =>
      setLogs((prev) => [entry, ...prev].slice(0, MAX_LOGS)),
    )
    connection.on('BacktestProgress', (u: BacktestProgressUpdate) => {
      setBacktestProgress((prev) => ({ ...prev, [u.jobId]: u }))
      if (u.stage === 'Completed' || u.stage === 'Failed') {
        setTimeout(() => {
          setBacktestProgress((prev) => {
            const { [u.jobId]: _removed, ...rest } = prev
            return rest
          })
        }, 8000)
      }
    })

    connection.onreconnected(() => setConnected(true))
    connection.onreconnecting(() => setConnected(false))
    connection.onclose(() => setConnected(false))

    connection
      .start()
      .then(() => setConnected(true))
      .catch(() => setConnected(false))

    return () => {
      connectionRef.current = null
      void connection.stop()
    }
  }, [])

  const value = useMemo<EngineHubState>(
    () => ({
      connected,
      prices,
      engineStatus,
      account,
      logs,
      tradeEventCount,
      lastTradeEvent,
      backtestProgress,
    }),
    [connected, prices, engineStatus, account, logs, tradeEventCount, lastTradeEvent, backtestProgress],
  )

  return <EngineHubContext.Provider value={value}>{children}</EngineHubContext.Provider>
}

// eslint-disable-next-line react-refresh/only-export-components
export function useEngineHub() {
  return useContext(EngineHubContext)
}
