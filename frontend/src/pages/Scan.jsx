import React from 'react'
import { useParams } from 'react-router-dom'
import useScanStore from '../stores/scanStore'
import ProgressPanel from '../components/ProgressPanel'
import TargetStatusList from '../components/TargetStatusList'
import ResultList from '../components/ResultList'

const terminalStatuses = new Set(['completed', 'failed'])

const Scan = () => {
  const { jobId } = useParams()
  const currentJob = useScanStore((state) => state.currentJob)
  const isFetching = useScanStore((state) => state.isFetching)
  const error = useScanStore((state) => state.error)
  const fetchJob = useScanStore((state) => state.fetchJob)
  const job = currentJob?.id === jobId ? currentJob : null

  React.useEffect(() => {
    if (!jobId) {
      return undefined
    }

    let isCancelled = false
    let timeoutId

    const loadJob = async () => {
      try {
        const nextJob = await fetchJob(jobId)

        if (!isCancelled && nextJob && !terminalStatuses.has(nextJob.status)) {
          timeoutId = window.setTimeout(loadJob, 2000)
        }
      } catch {
        if (!isCancelled) {
          timeoutId = window.setTimeout(loadJob, 4000)
        }
      }
    }

    loadJob()

    return () => {
      isCancelled = true
      if (timeoutId) {
        window.clearTimeout(timeoutId)
      }
    }
  }, [fetchJob, jobId])

  if (!job && isFetching) {
    return (
      <div className="rounded-2xl border border-zinc-800 bg-zinc-900/60 p-6 text-zinc-400">
        Loading scan job...
      </div>
    )
  }

  if (!job) {
    return (
      <div className="rounded-2xl border border-zinc-800 bg-zinc-900/60 p-6 text-zinc-400">
        {error || 'Scan job not found.'}
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <ProgressPanel status={job.status} progress={job.progress} />
      <section className="rounded-2xl border border-zinc-800 bg-zinc-900/60 p-5">
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div>
            <h2 className="text-lg font-medium">Query Summary</h2>
            <p className="mt-2 text-sm text-zinc-500">
              Job <span className="font-mono text-zinc-300">{job.id}</span>
            </p>
          </div>
          <p className="text-sm text-zinc-500">
            Updated {new Date(job.updatedAt).toLocaleString()}
          </p>
        </div>
        <div className="mt-4 grid gap-4 md:grid-cols-3">
          <div>
            <p className="text-xs uppercase tracking-wide text-zinc-500">Username</p>
            <p className="mt-1 text-sm text-zinc-200">{job.query.username || "-"}</p>
          </div>
          <div>
            <p className="text-xs uppercase tracking-wide text-zinc-500">Display Name</p>
            <p className="mt-1 text-sm text-zinc-200">{job.query.displayName || "-"}</p>
          </div>
          <div>
            <p className="text-xs uppercase tracking-wide text-zinc-500">Keywords</p>
            <p className="mt-1 text-sm text-zinc-200">
              {job.query.keywords?.length ? job.query.keywords.join(", ") : "-"}
            </p>
          </div>
        </div>
        {job.error ? (
          <p className="mt-4 rounded-xl border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-300">
            {job.error}
          </p>
        ) : null}
    </section>

      <div className="grid gap-6 lg:grid-cols-[0.9fr_1.1fr]">
        <TargetStatusList targets={job.targets} />
        <ResultList results={job.results} />
      </div>
    </div>
  )
}

export default Scan
