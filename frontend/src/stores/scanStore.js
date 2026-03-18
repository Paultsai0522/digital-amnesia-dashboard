import { create } from "zustand";
import { createScanJob, getScanJob } from "../api/jobs";

export const useScanStore = create((set) => ({
  currentJob: null,
  isSubmitting: false,
  isFetching: false,
  error: null,

  createJob: async (payload) => {
    set({
      isSubmitting: true,
      error: null,
    });

    try {
      const { job, jobId } = await createScanJob(payload);

      set({
        currentJob: job,
        isSubmitting: false,
      });

      return jobId;
    } catch (error) {
      set({
        isSubmitting: false,
        error: error instanceof Error ? error.message : "Failed to create scan job.",
      });

      throw error;
    }
  },

  fetchJob: async (jobId) => {
    set({
      isFetching: true,
    });

    try {
      const job = await getScanJob(jobId);

      set({
        currentJob: job,
        isFetching: false,
        error: null,
      });

      return job;
    } catch (error) {
      set({
        isFetching: false,
        error: error instanceof Error ? error.message : "Failed to load scan job.",
      });

      throw error;
    }
  },

  clearError: () => {
    set({ error: null });
  },
}));

export default useScanStore;
