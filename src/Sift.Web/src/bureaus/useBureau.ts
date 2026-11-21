import { useEffect, useState } from 'react';
import { apiClient } from '../api/client';
import type { Bureau } from './useBureaus';

export type BureauState =
  | { kind: 'loading' }
  | { kind: 'loaded'; bureau: Bureau }
  | { kind: 'not-found' }
  | { kind: 'failed'; detail: string };

export function useBureau(id: string): BureauState {
  const [result, setResult] = useState<{ id: string; state: BureauState } | null>(null);

  useEffect(() => {
    let cancelled = false;

    function finish(state: BureauState) {
      if (!cancelled) {
        setResult({ id, state });
      }
    }

    apiClient
      .GET('/bureaus/{id}', { params: { path: { id } } })
      .then(({ data, error, response }) => {
        if (data) {
          finish({ kind: 'loaded', bureau: data });
        } else if (response.status === 404) {
          finish({ kind: 'not-found' });
        } else {
          finish({ kind: 'failed', detail: error?.detail ?? error?.title ?? 'Could not load this bureau.' });
        }
      })
      .catch((reason: unknown) => {
        finish({ kind: 'failed', detail: reason instanceof Error ? reason.message : 'The API could not be reached.' });
      });

    return () => {
      cancelled = true;
    };
  }, [id]);

  return result && result.id === id ? result.state : { kind: 'loading' };
}
