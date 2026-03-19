import React from 'react'

const ProgressPanel = ({ status, progress, error }) => {
  const badgeStyles = {
    queued: 'border-zinc-700 bg-zinc-800 text-zinc-300',
    running: 'border-cyan-500/30 bg-cyan-500/10 text-cyan-300',
    completed: 'border-emerald-500/30 bg-emerald-500/10 text-emerald-300',
    failed: 'border-red-500/30 bg-red-500/10 text-red-300',
  }

  const helperText = {
    queued: 'Waiting for a worker to claim this job.',
    running: `${progress}% completed`,
    completed: error ? 'Scan finished with partial failures.' : 'Scan finished successfully.',
    failed: 'Scan stopped before completion.',
  }

  return (
    <div>
        <section className="rounded-2xl border border-zinc-800 bg-zinc-900/60 p-5">
        <div className="flex items-center justify-between">
            <h2 className="text-lg font-medium">Scan Progress</h2>
            <span className={`rounded-full border px-3 py-1 text-xs ${badgeStyles[status] ?? badgeStyles.queued}`}>
            {status}
            </span>
        </div>

        <div className="mt-4 h-3 overflow-hidden rounded-full bg-zinc-800">
            <div
            className="h-full rounded-full bg-cyan-400 transition-all"
            style={{ width: `${progress}%` }}
            />
        </div>

        <p className="mt-2 text-sm text-zinc-400">{helperText[status] ?? `${progress}% completed`}</p>
        </section>
    </div>
  )
}

export default ProgressPanel
