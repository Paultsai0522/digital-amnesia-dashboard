# Railway Demo Deploy

This repo is easiest to host as three Railway services:

1. `frontend` as a Docker service from `frontend/`
2. `backend` as a Docker service from `backend/`
3. `worker` as a Docker service from `worker/`

## Why this shape

- The frontend is a static Vite build served by nginx.
- The backend is an ASP.NET Core API.
- The worker is a separate long-running .NET process.
- The backend stores jobs in `backend/data/jobs.json`, so it needs a persistent volume.

## One-time setup

1. Push this repo to GitHub.
2. In Railway, create a new project from that GitHub repo.
3. Add three services from the same repo.
4. Set each service's Root Directory:
   - `frontend`
   - `backend`
   - `worker`

## Backend service

- Generate a Railway public domain for the backend.
- Add a volume mounted at `/app/data`.
- Keep the default start behavior from `backend/Dockerfile`.
- Optional environment variables:
  - `PORT=3001`

The backend stores its JSON data file at `/app/data/jobs.json` inside the container.

## Worker service

Set these environment variables:

- `BACKEND_API_URL=http://${{backend.RAILWAY_PRIVATE_DOMAIN}}`
- Optional: `WORKER_ID=railway-worker-1`
- Optional: `WORKER_POLL_INTERVAL_MS=2000`
- Optional: `WORKER_STEP_DELAY_MS=900`

The worker does not need a public domain.

## Frontend service

- Generate a Railway public domain for the frontend.
- Set:
  - `VITE_API_BASE_URL=https://${{backend.RAILWAY_PUBLIC_DOMAIN}}`

After changing `VITE_API_BASE_URL`, redeploy the frontend so Vite bakes the backend URL into the build.

## Expected result

- Visiting the frontend domain loads the React app.
- Creating a scan sends `POST /api/jobs` to the backend.
- The worker claims queued jobs over Railway private networking.
- Job state persists across backend restarts because `/app/data` is on a volume.

## Notes

- This is suitable for a demo, not for horizontal scaling.
- The current backend CORS policy is permissive and should be tightened before a public production launch.
- If you change the backend domain later, redeploy the frontend with the new `VITE_API_BASE_URL`.
