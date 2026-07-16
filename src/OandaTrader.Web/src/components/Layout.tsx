import { NavLink, Outlet } from 'react-router-dom'
import { useEffect, useState } from 'react'
import { useEngineHub } from '../hooks/EngineHubProvider'

type Theme = 'light' | 'dark' | 'system'

function useTheme() {
  const [theme, setTheme] = useState<Theme>(
    () => (localStorage.getItem('theme') as Theme | null) ?? 'system',
  )

  useEffect(() => {
    const root = document.documentElement
    if (theme === 'system') {
      root.removeAttribute('data-theme')
    } else {
      root.setAttribute('data-theme', theme)
    }
    localStorage.setItem('theme', theme)
  }, [theme])

  return { theme, setTheme }
}

export function Layout() {
  const { connected, engineStatus } = useEngineHub()
  const { theme, setTheme } = useTheme()

  const running = engineStatus?.engineEnabled === true && !engineStatus?.pausedReason
  const paused = Boolean(engineStatus?.pausedReason)

  // Status carries an icon + a text label, so the color is never the only channel.
  const statusLabel = paused ? 'Paused' : running ? 'Running' : 'Stopped'
  const statusIcon = paused ? '⚠' : running ? '●' : '■'
  const statusColor = paused
    ? 'var(--status-critical)'
    : running
      ? 'var(--status-good)'
      : 'var(--text-muted)'

  return (
    <div className="app-shell">
      <header className="app-header">
        <span className="app-brand">OandaTrader</span>
        <nav className="app-nav">
          <NavLink to="/" end>Dashboard</NavLink>
          <NavLink to="/trades">Trades</NavLink>
          <NavLink to="/analytics">Analytics</NavLink>
          <NavLink to="/model">Model</NavLink>
          <NavLink to="/settings">Settings</NavLink>
        </nav>

        <span className="pill" title={engineStatus?.pausedReason ?? statusLabel}>
          <span style={{ color: statusColor }} aria-hidden="true">{statusIcon}</span>
          Engine: {statusLabel}
        </span>

        <span className="pill" title={connected ? 'Live updates connected' : 'Live updates disconnected'}>
          <span
            className="pill-dot"
            style={{ background: connected ? 'var(--status-good)' : 'var(--text-muted)' }}
            aria-hidden="true"
          />
          {connected ? 'Live' : 'Offline'}
        </span>

        <select
          value={theme}
          onChange={(e) => setTheme(e.target.value as Theme)}
          style={{ width: 'auto' }}
          aria-label="Color theme"
        >
          <option value="system">System</option>
          <option value="light">Light</option>
          <option value="dark">Dark</option>
        </select>
      </header>

      <main className="app-main">
        <Outlet />
      </main>
    </div>
  )
}
