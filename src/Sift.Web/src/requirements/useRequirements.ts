import { useCallback, useEffect, useState } from 'react';
import { apiClient } from '../api/client';
import type { components } from '../api/generated/schema';

export type Requirement = components['schemas']['Requirement'];
export type RequirementKind = components['schemas']['RequirementKind'];
type ValidationProblem = components['schemas']['ValidationProblemDetails'];
type Problem = components['schemas']['ProblemDetails'];

export type RequirementsState =
  | { kind: 'loading' }
  | { kind: 'ready'; items: Requirement[] }
  | { kind: 'failed'; detail: string };

export type Outcome = { ok: true } | { ok: false; message: string };

const pageSize = 100;

function reasonMessage(reason: unknown, fallback: string): string {
  return reason instanceof Error ? reason.message : fallback;
}

function validationMessage(error: ValidationProblem | Problem | undefined, fallback: string): string {
  const errors = error && 'errors' in error ? error.errors : undefined;
  const first = errors ? Object.values(errors).flat()[0] : undefined;
  return first ?? error?.detail ?? error?.title ?? fallback;
}

// Requirements feed matching, so the editor shows all of them rather than one page.
async function loadAll(): Promise<{ items: Requirement[] } | { detail: string }> {
  const items: Requirement[] = [];

  for (let page = 1; ; page++) {
    const { data, error } = await apiClient.GET('/requirements', { params: { query: { page, pageSize } } });

    if (!data) {
      return { detail: error?.detail ?? error?.title ?? 'Could not load your requirements.' };
    }

    items.push(...data.items);

    if (data.items.length === 0 || items.length >= data.total) {
      return { items };
    }
  }
}

export function useRequirements() {
  const [state, setState] = useState<RequirementsState>({ kind: 'loading' });

  useEffect(() => {
    let cancelled = false;

    loadAll()
      .then((result) => {
        if (cancelled) {
          return;
        }

        setState('items' in result ? { kind: 'ready', items: result.items } : { kind: 'failed', detail: result.detail });
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
