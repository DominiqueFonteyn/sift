import { useEffect, useState } from 'react';
import { apiClient } from '../api/client';
import type { components } from '../api/generated/schema';

export type Bureau = components['schemas']['Bureau'];

export const bureausPageSize = 20;

export type BureausState =
  | { kind: 'loading' }
  | { kind: 'loaded'; items: Bureau[]; total: number }
  | { kind: 'failed'; detail: string };

// Changing reloadKey refetches the page, e.g. after a bureau was added.
export function useBureaus(page: number, reloadKey = 0): BureausState {
  const [result, setResult] = useState<{ key: string; state: BureausState } | null>(null);
  const key = `${page}:${reloadKey}`;

  useEffect(() => {
    let cancelled = false;

    function finish(state: BureausState) {
      if (!cancelled) {
        setResult({ key, state });
      }
    }

    apiClient
      .GET('/bureaus', { params: { query: { page, pageSize: bureausPageSize } } })
      .then(({ data, error }) => {
        finish(
          data
            ? { kind: 'loaded', items: data.items, total: data.total }
            : { kind: 'failed', detail: error?.detail ?? error?.title ?? 'Could not load your bureaus.' },
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
