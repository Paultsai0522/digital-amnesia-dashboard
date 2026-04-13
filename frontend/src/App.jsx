import React from 'react'
import { Outlet } from 'react-router-dom'

const App = () => {
  return (
    <div className="relative min-h-screen overflow-hidden text-[var(--ink)]">
      <div className="pointer-events-none fixed inset-0 opacity-80">
        <div className="absolute left-[-9rem] top-[-8rem] h-80 w-80 rounded-full bg-[#ead8be]/50 blur-3xl" />
        <div className="absolute right-[-7rem] top-24 h-96 w-96 rounded-full bg-[#fff6e8]/70 blur-3xl" />
        <div className="absolute bottom-[-10rem] left-1/2 h-96 w-96 -translate-x-1/2 rounded-full bg-[#dccab2]/35 blur-3xl" />
      </div>

      <div className="relative mx-auto max-w-6xl px-5 py-10 sm:px-8 lg:py-14">
        <header className="soft-rise mb-10 max-w-3xl">
          <p className="soft-kicker">
            Digital Amnesia Dashboard
          </p>
          <h1 className="font-display mt-3 text-4xl font-semibold leading-tight tracking-[-0.04em] text-[#322c25] sm:text-6xl">
            Identity Footprint Scanner
          </h1>
          <p className="mt-4 max-w-2xl text-base leading-7 text-[var(--muted)]">
            A calm workspace for queueing public identity scans, watching worker progress, and reviewing profile matches.
          </p>
        </header>
        <main className="soft-rise-delayed">
          <Outlet />
        </main>
      </div>
    </div>
  )
}

export default App
