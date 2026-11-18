import { useEffect, useState } from 'react';
import { apiClient } from '../api/client';
import type { components } from '../api/generated/schema';

type HealthStatus = components['schemas']['HealthStatus'];

export type HealthState =
  | { kind: 'loading' }
  | { kind: 'loaded'; status: HealthStatus }
  | { kind: 'failed'; detail: string };

export function useHealth(): HealthState {
  const [state, setState] = useState<HealthState>({ kind: 'loading' });

  useEffect(() => {
    let cancelled = false;

    apiClient
      .GET('/health')
      .then(({ data, error }) => {
        if (cancelled) {
          return;
        }

        if (data) {
          setState({ kind: 'loaded', status: data.status });
          return;
        }

        setState({ kind: 'failed', detail: error?.detail ?? error?.title ?? 'The API could not be reached.' });
      })
      .catch((reason: unknown) => {
        if (!cancelled) {
          setState({ kind: 'failed', detail: reason instanceof Error ? reason.message : 'The API could not be reached.' });
        }
      });

    return () => {
      cancelled = true;
    };
  }, []);

  return state;
}
