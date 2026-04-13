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
      <div className="soft-card rounded-[2rem] p-6 text-[var(--muted)]">
        Loading scan job...
      </div>
    )
  }

  if (!job) {
    return (
      <div className="soft-card rounded-[2rem] p-6 text-[var(--muted)]">
        {error || 'Scan job not found.'}
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <ProgressPanel status={job.status} progress={job.progress} error={job.error} />
      <section className="soft-card rounded-[2rem] p-5 sm:p-6">
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div>
            <p className="soft-kicker">Request</p>
            <h2 className="font-display mt-1 text-2xl font-semibold tracking-[-0.03em]">Query Summary</h2>
            <p className="mt-2 text-sm text-[var(--muted)]">
              Job <span className="font-mono text-[#5d5248]">{job.id}</span>
            </p>
          </div>
          <p className="rounded-full border border-[#dccfbe] bg-[#fff9ef] px-3 py-1 text-sm text-[var(--muted)]">
            Updated {new Date(job.updatedAt).toLocaleString()}
          </p>
        </div>
        <div className="mt-4 grid gap-4 md:grid-cols-3">
          <div className="soft-card-inset rounded-2xl p-4">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--quiet)]">Username</p>
            <p className="mt-1 text-sm font-semibold text-[#3b332b]">{job.query.username || "-"}</p>
          </div>
          <div className="soft-card-inset rounded-2xl p-4">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--quiet)]">Display Name</p>
            <p className="mt-1 text-sm font-semibold text-[#3b332b]">{job.query.displayName || "-"}</p>
          </div>
          <div className="soft-card-inset rounded-2xl p-4">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--quiet)]">Keywords</p>
            <p className="mt-1 text-sm font-semibold text-[#3b332b]">
              {job.query.keywords?.length ? job.query.keywords.join(", ") : "-"}
            </p>
          </div>
        </div>
        {job.error ? (
          <p className="mt-4 rounded-2xl border border-[#e7b5aa] bg-[#fff1ed] px-4 py-3 text-sm text-[var(--danger)]">
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
