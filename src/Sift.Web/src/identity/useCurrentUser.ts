import { useEffect, useState } from 'react';
import { apiClient } from '../api/client';
import type { components } from '../api/generated/schema';

type CurrentUser = components['schemas']['CurrentUser'];

export type CurrentUserState =
  | { kind: 'loading' }
  | { kind: 'loaded'; user: CurrentUser }
  | { kind: 'failed'; detail: string };

export function useCurrentUser(): CurrentUserState {
  const [state, setState] = useState<CurrentUserState>({ kind: 'loading' });

  useEffect(() => {
    let cancelled = false;

    apiClient
      .GET('/auth/me')
      .then(({ data, error }) => {
        if (cancelled) {
          return;
        }

        setState(
          data
            ? { kind: 'loaded', user: data }
            : { kind: 'failed', detail: error?.detail ?? error?.title ?? 'Could not load your account.' },
        );
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
