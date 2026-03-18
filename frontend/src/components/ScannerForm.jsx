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
        className="rounded-2xl border border-zinc-800 bg-zinc-900/60 p-6 shadow-2xl"
        >
            <div className="grid gap-5 md:grid-cols-2">
                <div>
                <label className="mb-2 block text-sm text-zinc-300">Username</label>
                <input
                    value={username}
                    onChange={(e) => setUsername(e.target.value)}
                    placeholder="e.g. Trump"
                    className="w-full rounded-xl border border-zinc-700 bg-zinc-950 px-4 py-3 text-sm outline-none focus:border-cyan-400"
                />
                </div>

                <div>
                <label className="mb-2 block text-sm text-zinc-300">Display Name</label>
                <input
                    value={displayName}
                    onChange={(e) => setDisplayName(e.target.value)}
                    placeholder="e.g. Donald Trump"
                    className="w-full rounded-xl border border-zinc-700 bg-zinc-950 px-4 py-3 text-sm outline-none focus:border-cyan-400"
                />
                </div>
            </div>

            <div className="mt-5">
                <label className="mb-2 block text-sm text-zinc-300">Keywords</label>
                <input
                value={keywords}
                onChange={(e) => setKeywords(e.target.value)}
                placeholder="us, president"
                className="w-full rounded-xl border border-zinc-700 bg-zinc-950 px-4 py-3 text-sm outline-none focus:border-cyan-400"
                />
                <p className="mt-2 text-xs text-zinc-500">
                Separate keywords with commas.
                </p>
            </div>

            {activeError ? (
              <p className="mt-4 rounded-xl border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-300">
                {activeError}
              </p>
            ) : null}

            <button
                type="submit"
                disabled={isSubmitting}
                className="mt-6 rounded-xl bg-cyan-400 px-5 py-3 text-sm font-medium text-zinc-950 transition hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-60"
            >
                {isSubmitting ? 'Queueing...' : 'Queue Scan'}
            </button>
        </form>
    </div>
  )
}

export default ScannerForm
