import React from 'react'
import { Outlet } from 'react-router-dom'

const App = () => {
  return (
    <div className="min-h-screen bg-zinc-950 text-zinc-100">
      <div className="mx-auto max-w-6xl px-6 py-10">
        <header className="mb-8">
          <p className="text-sm uppercase tracking-[0.2em] text-cyan-400">
            Digital Amnesia Dashboard
          </p>
          <h1 className="mt-2 text-3xl font-semibold">
            Identity Footprint Scanner
          </h1>
          <p className="mt-2 max-w-2xl text-sm text-zinc-400">
            Scan digital footprints and identify potential risks of digital amnesia.
          </p>
        </header>
        <Outlet />
      </div>
    </div>
  )
}

export default App
