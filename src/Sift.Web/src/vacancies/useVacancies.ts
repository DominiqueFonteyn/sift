import { useEffect, useState } from 'react';
import { apiClient } from '../api/client';
import type { components } from '../api/generated/schema';

export type VacancyListItem = components['schemas']['VacancyListItem'];

export const vacanciesPageSize = 20;

export type VacanciesState =
  | { kind: 'loading' }
  | { kind: 'loaded'; items: VacancyListItem[]; total: number }
  | { kind: 'failed'; detail: string };

// Changing reloadKey refetches the page, e.g. after a delete.
export function useVacancies(page: number, reloadKey = 0): VacanciesState {
  const [result, setResult] = useState<{ key: string; state: VacanciesState } | null>(null);
  const key = `${page}:${reloadKey}`;

  useEffect(() => {
    let cancelled = false;

    function finish(state: VacanciesState) {
      if (!cancelled) {
        setResult({ key, state });
      }
    }

    apiClient
      .GET('/vacancies', { params: { query: { page, pageSize: vacanciesPageSize } } })
      .then(({ data, error }) => {
        finish(
          data
            ? { kind: 'loaded', items: data.items, total: data.total }
            : { kind: 'failed', detail: error?.detail ?? error?.title ?? 'Could not load your vacancies.' },
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
