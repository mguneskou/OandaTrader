import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import './index.css'
import App from './App.tsx'
import { EngineHubProvider } from './hooks/EngineHubProvider'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      // Live pushes come over SignalR; this is just a safety net for anything not pushed.
      refetchInterval: 30_000,
      refetchOnWindowFocus: true,
    },
  },
})

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <EngineHubProvider>
        <BrowserRouter>
          <App />
        </BrowserRouter>
      </EngineHubProvider>
    </QueryClientProvider>
  </StrictMode>,
)
