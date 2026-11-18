import { useCallback, useEffect, useState } from 'react';
import { apiClient } from '../api/client';
import { validateResume } from './resumeRules';

export type ResumeState =
  | { kind: 'loading' }
  | { kind: 'ready'; text: string; updatedAt: string | null }
  | { kind: 'failed'; detail: string };

export type SaveOutcome = { ok: true } | { ok: false; message: string };

function reasonMessage(reason: unknown, fallback: string): string {
  return reason instanceof Error ? reason.message : fallback;
}

export function useResume() {
  const [state, setState] = useState<ResumeState>({ kind: 'loading' });

  useEffect(() => {
    let cancelled = false;

    apiClient
      .GET('/resume')
      .then(({ data, error, response }) => {
        if (cancelled) {
          return;
        }

        if (data) {
          setState({ kind: 'ready', text: data.text, updatedAt: data.updatedAt });
        } else if (response.status === 404) {
          setState({ kind: 'ready', text: '', updatedAt: null });
        } else {
          setState({ kind: 'failed', detail: error?.detail ?? error?.title ?? 'Could not load your resume.' });
        }
      })
      .catch((reason: unknown) => {
        if (!cancelled) {
          setState({ kind: 'failed', detail: reasonMessage(reason, 'The API could not be reached.') });
        }
      });

    return () => {
      cancelled = true;
    };
  }, []);

  const save = useCallback(async (text: string): Promise<SaveOutcome> => {
    const invalid = validateResume(text);
    if (invalid) {
      return { ok: false, message: invalid };
    }

    try {
      const { data, error } = await apiClient.PUT('/resume', { body: { text } });

      if (!data) {
        return { ok: false, message: error?.detail ?? error?.title ?? 'Could not save your resume.' };
      }

      setState({ kind: 'ready', text: data.text, updatedAt: data.updatedAt });
      return { ok: true };
    } catch (reason: unknown) {
      return { ok: false, message: reasonMessage(reason, 'The API could not be reached.') };
    }
  }, []);

  return { state, save };
}
