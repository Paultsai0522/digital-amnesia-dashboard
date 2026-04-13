import React from 'react'
import ScannerForm from '../components/ScannerForm'

const Home = () => {
  return (
    <div className="grid gap-6 lg:grid-cols-[1.15fr_0.85fr]">
      <ScannerForm />

      <section className="soft-card rounded-[2rem] p-6 sm:p-7">
        <p className="soft-kicker">Flow</p>
        <h2 className="font-display mt-2 text-2xl font-semibold tracking-[-0.03em]">How it works</h2>
        <ol className="mt-6 space-y-4 text-sm leading-6 text-[var(--muted)]">
          <li className="soft-card-inset rounded-2xl p-4"><span className="font-semibold text-[var(--accent-deep)]">1.</span> Enter keywords like username, display name.</li>
          <li className="soft-card-inset rounded-2xl p-4"><span className="font-semibold text-[var(--accent-deep)]">2.</span> Track progress while the scan is running.</li>
          <li className="soft-card-inset rounded-2xl p-4"><span className="font-semibold text-[var(--accent-deep)]">3.</span> Review the final matched public profiles once the job completes.</li>
        </ol>
      </section>
    </div>
  )
}

export default Home
