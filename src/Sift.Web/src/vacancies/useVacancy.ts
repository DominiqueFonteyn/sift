import { useEffect, useState } from 'react';
import { apiClient } from '../api/client';
import type { components } from '../api/generated/schema';

export type Vacancy = components['schemas']['Vacancy'];

export type VacancyState =
  | { kind: 'loading' }
  | { kind: 'loaded'; vacancy: Vacancy }
  | { kind: 'not-found' }
  | { kind: 'failed'; detail: string };

export function useVacancy(id: string): VacancyState {
  const [result, setResult] = useState<{ id: string; state: VacancyState } | null>(null);

  useEffect(() => {
    let cancelled = false;

    function finish(state: VacancyState) {
      if (!cancelled) {
        setResult({ id, state });
      }
    }

    apiClient
      .GET('/vacancies/{id}', { params: { path: { id } } })
      .then(({ data, error, response }) => {
        if (data) {
          finish({ kind: 'loaded', vacancy: data });
        } else if (response.status === 404) {
          finish({ kind: 'not-found' });
        } else {
          // A 401 also expires the session, which swaps this page for the login form.
          finish({ kind: 'failed', detail: error?.detail ?? error?.title ?? 'Could not load this vacancy.' });
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
