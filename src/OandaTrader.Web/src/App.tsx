import { useEffect, useState } from 'react'

interface HealthResponse {
  status: string
  timestampUtc: string
}

function App() {
  const [health, setHealth] = useState<HealthResponse | null>(null)
  const [healthError, setHealthError] = useState<string | null>(null)

  const [testingConnection, setTestingConnection] = useState(false)
  const [accountSummary, setAccountSummary] = useState<unknown>(null)
  const [connectionError, setConnectionError] = useState<string | null>(null)

  useEffect(() => {
    fetch('/api/health')
      .then((res) => {
        if (!res.ok) throw new Error(`Request failed: ${res.status}`)
        return res.json() as Promise<HealthResponse>
      })
      .then(setHealth)
      .catch((err) => setHealthError(String(err)))
  }, [])

  async function testConnection() {
    setTestingConnection(true)
    setConnectionError(null)
    setAccountSummary(null)
    try {
      const res = await fetch('/api/account/summary')
      const body = await res.json()
      if (!res.ok) {
        throw new Error(body?.error ?? `Request failed: ${res.status}`)
      }
      setAccountSummary(body)
    } catch (err) {
      setConnectionError(String(err instanceof Error ? err.message : err))
    } finally {
      setTestingConnection(false)
    }
  }

  return (
    <main style={{ fontFamily: 'sans-serif', padding: '2rem' }}>
      <h1>OandaTrader</h1>

      <section>
        <h2>Scaffold check: /api/health</h2>
        {healthError && <p style={{ color: 'crimson' }}>Error: {healthError}</p>}
        {!healthError && !health && <p>Loading...</p>}
        {health && <pre>{JSON.stringify(health, null, 2)}</pre>}
      </section>

      <section>
        <h2>Oanda connection</h2>
        <button onClick={testConnection} disabled={testingConnection}>
          {testingConnection ? 'Testing...' : 'Test Connection'}
        </button>
        {connectionError && <p style={{ color: 'crimson' }}>Error: {connectionError}</p>}
        {accountSummary !== null && (
          <pre>{JSON.stringify(accountSummary, null, 2)}</pre>
        )}
      </section>
    </main>
  )
}

export default App
