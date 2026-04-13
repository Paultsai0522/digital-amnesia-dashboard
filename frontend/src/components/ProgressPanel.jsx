import React from 'react'

const ProgressPanel = ({ status, progress, error }) => {
  const badgeStyles = {
    queued: 'border-stone-300 bg-stone-100 text-stone-600',
    running: 'border-amber-200 bg-amber-50 text-amber-800',
    completed: 'border-emerald-200 bg-emerald-50 text-emerald-700',
    failed: 'border-rose-200 bg-rose-50 text-rose-700',
  }

  const helperText = {
    queued: 'Waiting for a worker to claim this job.',
    running: `${progress}% completed`,
    completed: error ? 'Scan finished with partial failures.' : 'Scan finished successfully.',
    failed: 'Scan stopped before completion.',
  }

  return (
    <div>
        <section className="soft-card rounded-[2rem] p-5 sm:p-6">
        <div className="flex items-center justify-between gap-4">
            <div>
              <p className="soft-kicker">Worker</p>
              <h2 className="font-display mt-1 text-2xl font-semibold tracking-[-0.03em]">Scan Progress</h2>
            </div>
            <span className={`rounded-full border px-3 py-1 text-xs font-semibold uppercase tracking-[0.18em] ${badgeStyles[status] ?? badgeStyles.queued}`}>
            {status}
            </span>
        </div>

        <div className="mt-5 h-3 overflow-hidden rounded-full bg-[#e5d8c8]">
            <div
            className="h-full rounded-full bg-gradient-to-r from-[#8a6f4f] via-[#b69669] to-[#ddc596] transition-all duration-500"
            style={{ width: `${progress}%` }}
            />
        </div>

        <p className="mt-3 text-sm text-[var(--muted)]">{helperText[status] ?? `${progress}% completed`}</p>
        </section>
    </div>
  )
}

export default ProgressPanel
