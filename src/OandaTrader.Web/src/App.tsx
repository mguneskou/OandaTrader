import { Route, Routes } from 'react-router-dom'
import { Layout } from './components/Layout'
import { Dashboard } from './pages/Dashboard'
import { TradeHistory } from './pages/TradeHistory'
import { Analytics } from './pages/Analytics'
import { Model } from './pages/Model'
import { SettingsPage } from './pages/SettingsPage'

function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route index element={<Dashboard />} />
        <Route path="trades" element={<TradeHistory />} />
        <Route path="analytics" element={<Analytics />} />
        <Route path="model" element={<Model />} />
        <Route path="settings" element={<SettingsPage />} />
      </Route>
    </Routes>
  )
}

export default App
