const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? "";

const request = async (path, options = {}) => {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    headers: {
      "Content-Type": "application/json",
      ...(options.headers ?? {}),
    },
    ...options,
  });

  const rawBody = await response.text();
  const data = rawBody ? JSON.parse(rawBody) : null;

  if (!response.ok) {
    throw new Error(data?.error ?? `Request failed with status ${response.status}.`);
  }

  return data;
};

export const createScanJob = async (payload) =>
  request("/api/jobs", {
    method: "POST",
    body: JSON.stringify(payload),
  });

export const getScanJob = async (jobId) => request(`/api/jobs/${jobId}`);
