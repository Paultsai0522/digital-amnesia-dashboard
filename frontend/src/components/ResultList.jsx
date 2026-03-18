import React from 'react'
import ResultCard from './ResultCard'

const ResultList = ({ results }) => {
  return (
    <section>
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-lg font-medium">Matches</h2>
        <p className="text-sm text-zinc-500">{results.length} result(s)</p>
      </div>

      {results.length > 0 ? (
        <div className="grid gap-4">
          {results.map((result) => (
            <ResultCard key={result.id} result={result} />
          ))}
        </div>
      ) : (
        <div className="rounded-2xl border border-dashed border-zinc-800 bg-zinc-900/40 p-5 text-sm text-zinc-500">
          No matches yet. Results will appear here as the worker finishes each target.
        </div>
      )}
    </section>
  )
}

export default ResultList
