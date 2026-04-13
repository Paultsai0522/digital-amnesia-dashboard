import React from 'react'

const ResultCard = ({ result }) => {
  const badgeStyles = {
    high: 'border-emerald-200 bg-emerald-50 text-emerald-700',
    medium: 'border-amber-200 bg-amber-50 text-amber-800',
    low: 'border-rose-200 bg-rose-50 text-rose-700',
  }

  return (
    <div>
        <article className="soft-card rounded-[2rem] p-5 transition duration-200 hover:-translate-y-0.5 hover:shadow-[0_28px_80px_rgba(101,82,58,0.16)]">
        <div className="flex items-start justify-between gap-4">
            <div>
            <p className="soft-kicker">
                {result.platform}
            </p>
            <h3 className="font-display mt-1 text-2xl font-semibold tracking-[-0.03em] text-[#322c25]">{result.username}</h3>
            <p className="mt-1 text-sm text-[var(--muted)]">{result.displayName}</p>
            </div>

            <span
            className={`rounded-full border px-3 py-1 text-xs font-semibold ${badgeStyles[result.matchLevel]}`}
            >
            {result.matchLevel} confidence
            </span>
        </div>

        <p className="mt-4 text-sm leading-6 text-[#50473e]">{result.bio}</p>

        <div className="mt-4">
            <p className="text-sm font-semibold text-[var(--accent-deep)]">Match Reasons</p>
            <ul className="mt-2 list-disc space-y-1 pl-5 text-sm text-[var(--muted)]">
            {result.matchReasons.map((reason) => (
                <li key={reason}>{reason}</li>
            ))}
            </ul>
        </div>

        <a
            href={result.profileUrl}
            target="_blank"
            rel="noreferrer"
            className="mt-4 inline-flex items-center rounded-full border border-[#d8c9b8] bg-[#fff9ef] px-4 py-2 text-sm font-semibold text-[var(--accent-deep)] transition hover:border-[#bda789] hover:bg-white"
        >
            Open profile
        </a>
        </article>
    </div>
  )
}

export default ResultCard
