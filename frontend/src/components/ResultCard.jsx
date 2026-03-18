import React from 'react'

const ResultCard = ({ result }) => {
  const badgeStyles = {
    high: 'border-green-500 bg-green-500/20 text-green-400',
    medium: 'border-yellow-500 bg-yellow-500/20 text-yellow-400',
    low: 'border-red-500 bg-red-500/20 text-red-400',
  }

  return (
    <div>
        <article className="rounded-2xl border border-zinc-800 bg-zinc-900/60 p-5">
        <div className="flex items-start justify-between gap-4">
            <div>
            <p className="text-xs uppercase tracking-[0.2em] text-cyan-400">
                {result.platform}
            </p>
            <h3 className="mt-1 text-lg font-medium">{result.username}</h3>
            <p className="mt-1 text-sm text-zinc-400">{result.displayName}</p>
            </div>

            <span
            className={`rounded-full border px-3 py-1 text-xs ${badgeStyles[result.matchLevel]}`}
            >
            {result.matchLevel} confidence
            </span>
        </div>

        <p className="mt-4 text-sm text-zinc-300">{result.bio}</p>

        <div className="mt-4">
            <p className="text-sm font-medium text-zinc-200">Match Reasons</p>
            <ul className="mt-2 list-disc space-y-1 pl-5 text-sm text-zinc-400">
            {result.matchReasons.map((reason) => (
                <li key={reason}>{reason}</li>
            ))}
            </ul>
        </div>

        <a
            href={result.profileUrl}
            target="_blank"
            rel="noreferrer"
            className="mt-4 inline-block text-sm text-cyan-400 hover:underline"
        >
            Open profile
        </a>
        </article>
    </div>
  )
}

export default ResultCard
