import { useEffect, useState } from 'react';
import { apiClient } from '../api/client';
import type { components } from '../api/generated/schema';

export type Recruiter = components['schemas']['Recruiter'];

export const recruitersPageSize = 20;

export type RecruitersState =
  | { kind: 'loading' }
  | { kind: 'loaded'; items: Recruiter[]; total: number }
  | { kind: 'failed'; detail: string };

// Changing reloadKey refetches the page, e.g. after a recruiter was added.
export function useRecruiters(page: number, reloadKey = 0): RecruitersState {
  const [result, setResult] = useState<{ key: string; state: RecruitersState } | null>(null);
  const key = `${page}:${reloadKey}`;

  useEffect(() => {
    let cancelled = false;

    function finish(state: RecruitersState) {
      if (!cancelled) {
        setResult({ key, state });
      }
    }

    apiClient
      .GET('/recruiters', { params: { query: { page, pageSize: recruitersPageSize } } })
      .then(({ data, error }) => {
        finish(
          data
            ? { kind: 'loaded', items: data.items, total: data.total }
            : { kind: 'failed', detail: error?.detail ?? error?.title ?? 'Could not load your recruiters.' },
        );
      })
      .catch((reason: unknown) => {
        finish({ kind: 'failed', detail: reason instanceof Error ? reason.message : 'The API could not be reached.' });
      });

    return () => {
      cancelled = true;
    };
  }, [page, key]);

  return result && result.key === key ? result.state : { kind: 'loading' };
}
