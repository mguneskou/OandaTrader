import { useEffect, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api, GRANULARITIES, type Granularity } from '../api/client'

interface FormState {
  riskPercentPerTrade: number
  granularity: Granularity
  maxDailyLossPercent: number
  maxConcurrentPositions: number
  maxTradesPerDay: number
  mlConfidenceThresholdPercent: number
  retrainAfterTradeCount: number
}

export function SettingsPage() {
  const queryClient = useQueryClient()
  const settings = useQuery({ queryKey: ['settings'], queryFn: api.getSettings })

  const [form, setForm] = useState<FormState | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  // Seed the form once the server state arrives (and whenever it's refetched).
  useEffect(() => {
    if (!settings.data) return
    const s = settings.data.settings
    setForm({
      riskPercentPerTrade: s.riskPercentPerTrade,
      granularity: s.granularity,
      maxDailyLossPercent: s.maxDailyLossPercent,
      maxConcurrentPositions: s.maxConcurrentPositions,
      maxTradesPerDay: s.maxTradesPerDay,
      mlConfidenceThresholdPercent: Math.round(s.mlConfidenceThreshold * 100),
      retrainAfterTradeCount: s.retrainAfterTradeCount,
    })
  }, [settings.data])

  const save = useMutation({
    mutationFn: (f: FormState) =>
      api.updateSettings({
        riskPercentPerTrade: f.riskPercentPerTrade,
        granularity: f.granularity,
        maxDailyLossPercent: f.maxDailyLossPercent,
        maxConcurrentPositions: f.maxConcurrentPositions,
        maxTradesPerDay: f.maxTradesPerDay,
        mlConfidenceThreshold: f.mlConfidenceThresholdPercent / 100,
        retrainAfterTradeCount: f.retrainAfterTradeCount,
      }),
    onMutate: () => {
      setError(null)
      setNotice(null)
    },
    onSuccess: () => {
      setNotice('Settings saved. The engine picks them up on its next tick — no restart needed.')
      void queryClient.invalidateQueries({ queryKey: ['settings'] })
    },
    onError: (e: Error) => setError(e.message),
  })

  const toggleInstrument = useMutation({
    mutationFn: ({ instrument, enabled }: { instrument: string; enabled: boolean }) =>
      api.updateInstrument(instrument, enabled),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['settings'] })
    },
    onError: (e: Error) => setError(e.message),
  })

  const update = <K extends keyof FormState>(key: K, value: FormState[K]) =>
    setForm((prev) => (prev ? { ...prev, [key]: value } : prev))

  if (settings.isLoading || !form) {
    return <div className="empty">Loading settings…</div>
  }

  return (
    <>
      <div className="page-header">
        <div>
          <h1>Settings</h1>
          <div className="page-subtitle">
            All values are read live by the trading engine on its next tick.
          </div>
        </div>
      </div>

      {error && (
        <div className="banner banner-critical">
          <span className="banner-icon" aria-hidden="true">!</span>
          <div>{error}</div>
        </div>
      )}
      {notice && (
        <div className="banner banner-good">
          <span className="banner-icon" aria-hidden="true">✓</span>
          <div>{notice}</div>
        </div>
      )}

      <div className="grid grid-2">
        <div className="card">
          <div className="card-title">Strategy &amp; sizing</div>

          <div className="field">
            <label htmlFor="risk">Risk per trade (% of account equity)</label>
            <input
              id="risk"
              type="number"
              step={0.1}
              min={0.1}
              max={10}
              value={form.riskPercentPerTrade}
              onChange={(e) => update('riskPercentPerTrade', Number(e.target.value))}
            />
            <div className="field-hint">
              Position size = (equity × this %) ÷ stop-loss distance.
            </div>
          </div>

          <div className="field">
            <label htmlFor="granularity">Candle granularity</label>
            <select
              id="granularity"
              value={form.granularity}
              onChange={(e) => update('granularity', e.target.value as Granularity)}
            >
              {GRANULARITIES.map((g) => (
                <option key={g} value={g}>
                  {g}
                </option>
              ))}
            </select>
            <div className="field-hint">
              The timeframe signals are evaluated on. Changing this invalidates the current
              model's training data — re-run backtests and retrain after switching.
            </div>
          </div>

          <div className="field">
            <label htmlFor="confidence">ML confidence threshold (%)</label>
            <input
              id="confidence"
              type="number"
              step={1}
              min={0}
              max={100}
              value={form.mlConfidenceThresholdPercent}
              onChange={(e) => update('mlConfidenceThresholdPercent', Number(e.target.value))}
            />
            <div className="field-hint">
              A signal is only traded when the model's predicted win probability is at least this
              high. Higher means fewer, more selective trades.
            </div>
          </div>

          <div className="field">
            <label htmlFor="retrain">Retrain after N closed live trades</label>
            <input
              id="retrain"
              type="number"
              step={1}
              min={1}
              value={form.retrainAfterTradeCount}
              onChange={(e) => update('retrainAfterTradeCount', Number(e.target.value))}
            />
            <div className="field-hint">
              How the bot learns from its mistakes: every N closed trades it refits on the
              accumulated outcomes and promotes the new model only if it isn't worse.
            </div>
          </div>
        </div>

        <div className="card">
          <div className="card-title">Circuit breakers</div>
          <div className="card-subtitle">
            When any limit is hit, trading auto-pauses until you resume it from the dashboard.
          </div>

          <div className="field">
            <label htmlFor="maxloss">Max daily loss (% of start-of-day equity)</label>
            <input
              id="maxloss"
              type="number"
              step={0.5}
              min={0.5}
              value={form.maxDailyLossPercent}
              onChange={(e) => update('maxDailyLossPercent', Number(e.target.value))}
            />
          </div>

          <div className="field">
            <label htmlFor="maxpos">Max concurrent open positions</label>
            <input
              id="maxpos"
              type="number"
              step={1}
              min={1}
              value={form.maxConcurrentPositions}
              onChange={(e) => update('maxConcurrentPositions', Number(e.target.value))}
            />
          </div>

          <div className="field">
            <label htmlFor="maxtrades">Max trades per day</label>
            <input
              id="maxtrades"
              type="number"
              step={1}
              min={1}
              value={form.maxTradesPerDay}
              onChange={(e) => update('maxTradesPerDay', Number(e.target.value))}
            />
          </div>
        </div>
      </div>

      <div className="card" style={{ marginBottom: '1rem' }}>
        <div className="card-title">Instruments</div>
        <div className="card-subtitle">
          Only enabled instruments are streamed and traded.
        </div>
        {settings.data?.instruments.map((i) => (
          <label className="checkbox-row" key={i.instrument}>
            <input
              type="checkbox"
              checked={i.enabled}
              disabled={toggleInstrument.isPending}
              onChange={(e) =>
                toggleInstrument.mutate({ instrument: i.instrument, enabled: e.target.checked })
              }
            />
            {i.instrument}
          </label>
        ))}
      </div>

      <div className="btn-row">
        <button
          className="btn btn-primary"
          onClick={() => save.mutate(form)}
          disabled={save.isPending}
        >
          {save.isPending ? 'Saving…' : 'Save settings'}
        </button>
      </div>
    </>
  )
}
