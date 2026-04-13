import React from 'react'
import { useNavigate } from 'react-router-dom'
import useScanStore from '../stores/scanStore'

const ScannerForm = () => {
  const navigate = useNavigate()
  const createJob = useScanStore((state) => state.createJob)
  const isSubmitting = useScanStore((state) => state.isSubmitting)
  const storeError = useScanStore((state) => state.error)
  const clearError = useScanStore((state) => state.clearError)

  const [username, setUsername] = React.useState('')
  const [displayName, setDisplayName] = React.useState('')
  const [keywords, setKeywords] = React.useState('')
  const [validationError, setValidationError] = React.useState('')

  const handleSubmit = async (e) => {
    e.preventDefault()
    clearError()

    const normalizedPayload = {
      username: username.trim(),
      displayName: displayName.trim(),
      keywords: keywords.split(',').map((item) => item.trim()).filter(Boolean),
    }

    if (
      !normalizedPayload.username &&
      !normalizedPayload.displayName &&
      normalizedPayload.keywords.length === 0
    ) {
      setValidationError('Provide at least one identity signal before starting a scan.')
      return
    }

    setValidationError('')

    try {
      const jobId = await createJob(normalizedPayload)
      navigate('/scan/' + jobId)
    } catch {
      return
    }
  }

  const activeError = validationError || storeError

  return (
    <div>
        <form
        onSubmit={handleSubmit}
        className="soft-card rounded-[2rem] p-6 sm:p-7"
        >
            <div className="mb-6">
              <p className="soft-kicker">New Scan</p>
              <h2 className="font-display mt-2 text-2xl font-semibold tracking-[-0.03em] text-[#322c25]">
                Start with the soft signals.
              </h2>
              <p className="mt-2 max-w-xl text-sm leading-6 text-[var(--muted)]">
                Add one or more hints. The worker will use them to search the configured public platforms.
              </p>
            </div>

            <div className="grid gap-5 md:grid-cols-2">
                <div>
                <label className="mb-2 block text-sm font-semibold text-[var(--accent-deep)]">Username</label>
                <input
                    value={username}
                    onChange={(e) => setUsername(e.target.value)}
                    placeholder="e.g. trump"
                    className="soft-input rounded-2xl px-4 py-3 text-sm outline-none"
                />
                </div>

                <div>
                <label className="mb-2 block text-sm font-semibold text-[var(--accent-deep)]">Display Name</label>
                <input
                    value={displayName}
                    onChange={(e) => setDisplayName(e.target.value)}
                    placeholder="e.g. Donald Trump"
                    className="soft-input rounded-2xl px-4 py-3 text-sm outline-none"
                />
                </div>
            </div>

            <div className="mt-5">
                <label className="mb-2 block text-sm font-semibold text-[var(--accent-deep)]">Keywords</label>
                <input
                value={keywords}
                onChange={(e) => setKeywords(e.target.value)}
                placeholder="president, usa, golf"
                className="soft-input rounded-2xl px-4 py-3 text-sm outline-none"
                />
                <p className="mt-2 text-xs text-[var(--quiet)]">
                Separate keywords with commas.
                </p>
            </div>

            {activeError ? (
              <p className="mt-4 rounded-2xl border border-[#e7b5aa] bg-[#fff1ed] px-4 py-3 text-sm text-[var(--danger)]">
                {activeError}
              </p>
            ) : null}

            <button
                type="submit"
                disabled={isSubmitting}
                className="soft-button mt-6 rounded-2xl px-5 py-3 text-sm font-semibold disabled:cursor-not-allowed disabled:opacity-60"
            >
                {isSubmitting ? 'Queueing...' : 'Queue Scan'}
            </button>
        </form>
    </div>
  )
}

export default ScannerForm
