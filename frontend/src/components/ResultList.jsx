import React from 'react'
import ResultCard from './ResultCard'

const ResultList = ({ results }) => {
  return (
    <section>
      <div className="mb-4 flex items-center justify-between">
        <div>
          <p className="soft-kicker">Review</p>
          <h2 className="font-display mt-1 text-2xl font-semibold tracking-[-0.03em]">Matches</h2>
        </div>
        <p className="rounded-full border border-[#dccfbe] bg-[#fff9ef] px-3 py-1 text-sm font-semibold text-[var(--muted)]">{results.length} result(s)</p>
      </div>

      {results.length > 0 ? (
        <div className="grid gap-4">
          {results.map((result) => (
            <ResultCard key={result.id} result={result} />
          ))}
        </div>
      ) : (
        <div className="rounded-[2rem] border border-dashed border-[#d8c9b8] bg-[#fffaf2]/70 p-5 text-sm text-[var(--muted)]">
          No matches yet. Results will appear here as the worker finishes each target.
        </div>
      )}
    </section>
  )
}

export default ResultList
