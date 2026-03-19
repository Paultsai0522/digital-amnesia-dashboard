# Digital Amnesia Dashboard

Digital Amnesia Dashboard is a demo application for running asynchronous identity scan jobs.

## Stack

- `frontend/`: React + Vite + Tailwind
- `backend/`: ASP.NET Core minimal API
- `worker/`: .NET background worker service
- storage: JSON

## Project Layout

```text
|- backend/
|- frontend/
|- scripts/
|- worker/
|- digital-amnesia-dashboard.sln
`- package.json
```

## How It Works

A real queue pipeline and mixed scan providers:

- the frontend creates a scan job
- the backend stores the job in a queue
- the worker claims queued jobs and updates progress
- GitHub uses a live public scan path through the GitHub REST API
- Reddit and X remain mocked


## Requirements

- Node.js 22+
- npm 10+
- .NET 10 SDK

## Current Status

### Implemented
- Frontend scanning UI (form, progress, results)
- Job creation API
- Worker-based job processing (mock / initial version)
- Platform-by-platform progress tracking

### TODO
- Real platform scanning 
- Improved matching algorithm

## API Summary

Public routes:

- `GET /health`
- `POST /api/jobs`
- `GET /api/jobs/{jobId}`

Internal worker routes:

- `POST /internal/jobs/claim`
- `PATCH /internal/jobs/{jobId}`

## Privacy & Ethics

This project:
- Only accesses publicly available data
- Does not bypass login systems or protections

## Limitations

- only GitHub uses a live external scanner
- Reddit and X are still mocked
- backend persistence is a local JSON file
- the backend should not be horizontally scaled in this form
- internal worker endpoints are unauthenticated for demo purposes
