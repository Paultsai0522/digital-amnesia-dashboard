import React from 'react'
import ScannerForm from '../components/ScannerForm'

const Home = () => {
  return (
    <div className="grid gap-6 lg:grid-cols-[1.2fr_0.8fr]">
      <ScannerForm />

      <section className="rounded-2xl border border-zinc-800 bg-zinc-900/60 p-6">
        <h2 className="text-lg font-medium">How it works</h2>
        <ol className="mt-4 space-y-3 text-sm text-zinc-400">
          <li>1. Enter identity signals like username and keywords.</li>
          <li>2. The Job Producer API stores a queued scan job and returns a job id.</li>
          <li>3. A worker claims queued jobs and runs a live GitHub lookup plus demo scans for the other platforms.</li>
          <li>4. Review the final matched public profiles once the job completes.</li>
        </ol>
      </section>
    </div>
  )
}

export default Home
