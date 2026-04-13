import React from 'react'

const TargetStatusList = ({targets}) => {
  const statusStyles = {
    queued: 'text-stone-500',
    running: 'text-amber-700',
    completed: 'text-emerald-700',
    failed: 'text-rose-700',
  }

  return (
    <div>
        <section className="soft-card rounded-[2rem] p-5 sm:p-6">
        <p className="soft-kicker">Targets</p>
        <h2 className="font-display mt-1 text-2xl font-semibold tracking-[-0.03em]">Platforms</h2>

        <div className="mt-4 space-y-3">
            {targets.map((target) => (
            <div
                key={target.platform}
                className="soft-card-inset rounded-2xl p-4"
            >
                <div className="flex items-center justify-between">
                <p className="font-semibold text-[#3b332b]">{target.platform}</p>
                <span className={`text-xs font-semibold uppercase tracking-[0.18em] ${statusStyles[target.status] ?? statusStyles.queued}`}>
                    {target.status}
                </span>
                </div>
                <p className="mt-2 text-sm text-[var(--muted)]">{target.message}</p>
            </div>
            ))}
        </div>
        </section>
    </div>
  )
}

export default TargetStatusList
