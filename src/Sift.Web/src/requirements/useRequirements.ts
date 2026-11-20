import { useCallback, useEffect, useState } from 'react';
import { apiClient } from '../api/client';
import type { components } from '../api/generated/schema';

export type Requirement = components['schemas']['Requirement'];
export type RequirementKind = components['schemas']['RequirementKind'];

export type RequirementsState =
  | { kind: 'loading' }
  | { kind: 'ready'; items: Requirement[] }
  | { kind: 'failed'; detail: string };

export type Outcome = { ok: true } | { ok: false; message: string };

const pageSize = 100;

function reasonMessage(reason: unknown, fallback: string): string {
  return reason instanceof Error ? reason.message : fallback;
}

type ProblemLike = { title?: string; detail?: string; errors?: Record<string, string[]> };

function validationMessage(error: ProblemLike | undefined, fallback: string): string {
  const first = error?.errors ? Object.values(error.errors).flat()[0] : undefined;
  return first ?? error?.detail ?? error?.title ?? fallback;
}

export function useRequirements() {
  const [state, setState] = useState<RequirementsState>({ kind: 'loading' });

  useEffect(() => {
    let cancelled = false;

    apiClient
      .GET('/requirements', { params: { query: { pageSize } } })
      .then(({ data, error }) => {
        if (cancelled) {
          return;
        }

        if (data) {
          setState({ kind: 'ready', items: data.items });
        } else {
          setState({ kind: 'failed', detail: error?.detail ?? error?.title ?? 'Could not load your requirements.' });
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

  const add = useCallback(async (text: string, kind: RequirementKind): Promise<Outcome> => {
    if (text.trim() === '') {
      return { ok: false, message: 'A requirement needs text.' };
    }

    try {
      const { data, error } = await apiClient.POST('/requirements', { body: { text, kind } });

      if (!data) {
        return { ok: false, message: validationMessage(error, 'Could not add the requirement.') };
      }

      setState((current) => (current.kind === 'ready' ? { kind: 'ready', items: [...current.items, data] } : current));
      return { ok: true };
    } catch (reason: unknown) {
      return { ok: false, message: reasonMessage(reason, 'The API could not be reached.') };
    }
  }, []);

  const remove = useCallback(async (id: string): Promise<Outcome> => {
    try {
      const { response, error } = await apiClient.DELETE('/requirements/{id}', { params: { path: { id } } });

      if (!response.ok) {
        return { ok: false, message: error?.detail ?? error?.title ?? 'Could not delete the requirement.' };
      }

      setState((current) =>
        current.kind === 'ready' ? { kind: 'ready', items: current.items.filter((item) => item.id !== id) } : current,
      );
      return { ok: true };
    } catch (reason: unknown) {
      return { ok: false, message: reasonMessage(reason, 'The API could not be reached.') };
    }
  }, []);

  return { state, add, remove };
}
