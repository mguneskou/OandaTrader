import { useEffect, useState } from 'react'

interface HealthResponse {
  status: string
  timestampUtc: string
}

function App() {
  const [health, setHealth] = useState<HealthResponse | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    fetch('/api/health')
      .then((res) => {
        if (!res.ok) throw new Error(`Request failed: ${res.status}`)
        return res.json() as Promise<HealthResponse>
      })
      .then(setHealth)
      .catch((err) => setError(String(err)))
  }, [])

  return (
    <main style={{ fontFamily: 'sans-serif', padding: '2rem' }}>
      <h1>OandaTrader</h1>
      <p>Scaffold check: API connectivity via /api/health</p>
      {error && <p style={{ color: 'crimson' }}>Error: {error}</p>}
      {!error && !health && <p>Loading...</p>}
      {health && (
        <pre>{JSON.stringify(health, null, 2)}</pre>
      )}
    </main>
  )
}

export default App
