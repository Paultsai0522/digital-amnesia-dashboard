import React from 'react'

const TargetStatusList = ({targets}) => {
  const statusStyles = {
    queued: 'text-zinc-400',
    running: 'text-cyan-300',
    completed: 'text-emerald-300',
    failed: 'text-red-300',
  }

  return (
    <div>
        <section className="rounded-2xl border border-zinc-800 bg-zinc-900/60 p-5">
        <h2 className="text-lg font-medium">Platforms</h2>

        <div className="mt-4 space-y-3">
            {targets.map((target) => (
            <div
                key={target.platform}
                className="rounded-xl border border-zinc-800 bg-zinc-950/60 p-4"
            >
                <div className="flex items-center justify-between">
                <p className="font-medium">{target.platform}</p>
                <span className={`text-xs uppercase tracking-wide ${statusStyles[target.status] ?? statusStyles.queued}`}>
                    {target.status}
                </span>
                </div>
                <p className="mt-2 text-sm text-zinc-500">{target.message}</p>
            </div>
            ))}
        </div>
        </section>
    </div>
  )
}

export default TargetStatusList
