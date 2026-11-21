import { useEffect, useState } from 'react';
import { apiClient } from '../api/client';
import type { components } from '../api/generated/schema';

export type BureauChoice = components['schemas']['Bureau'];

// The API caps a page at 100, so the picker walks every page to offer all of the user's bureaus.
const choicesPageSize = 100;

export type BureauChoicesState =
  | { kind: 'loading' }
  | { kind: 'loaded'; items: BureauChoice[] }
  | { kind: 'failed'; detail: string };

async function loadAll(): Promise<BureauChoicesState> {
  const items: BureauChoice[] = [];

  for (let page = 1; ; page++) {
    const { data, error } = await apiClient.GET('/bureaus', { params: { query: { page, pageSize: choicesPageSize } } });

    if (!data) {
      return { kind: 'failed', detail: error?.detail ?? error?.title ?? 'Could not load your bureaus.' };
    }

    items.push(...data.items);
    if (data.items.length === 0 || items.length >= data.total) {
      return { kind: 'loaded', items };
    }
  }
}

// Changing reloadKey refetches in the background; the previous choices stay until the new ones arrive.
export function useBureauChoices(reloadKey: number): BureauChoicesState {
  const [state, setState] = useState<BureauChoicesState>({ kind: 'loading' });

  useEffect(() => {
    let cancelled = false;

    loadAll()
      .catch(
        (reason: unknown): BureauChoicesState => ({
          kind: 'failed',
          detail: reason instanceof Error ? reason.message : 'The API could not be reached.',
        }),
      )
      .then((next) => {
        if (!cancelled) {
          setState((previous) => (next.kind === 'failed' && previous.kind === 'loaded' ? previous : next));
        }
      });

    return () => {
      cancelled = true;
    };
  }, [reloadKey]);

  return state;
}
