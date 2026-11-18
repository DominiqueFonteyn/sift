import { useCallback, useState } from 'react';
import { apiClient } from '../api/client';
import type { components } from '../api/generated/schema';

type RegisterRequest = components['schemas']['RegisterRequest'];
type RegisteredUser = components['schemas']['RegisteredUser'];

export type FieldErrors = Partial<Record<keyof RegisterRequest, string[]>>;

export type RegisterState =
  | { kind: 'idle' }
  | { kind: 'submitting' }
  | { kind: 'registered'; user: RegisteredUser }
  | { kind: 'failed'; message: string; fieldErrors: FieldErrors };

const unreachable = 'The API could not be reached.';

export function useRegister(): { state: RegisterState; register: (request: RegisterRequest) => Promise<void> } {
  const [state, setState] = useState<RegisterState>({ kind: 'idle' });

  const register = useCallback(async (request: RegisterRequest) => {
    setState({ kind: 'submitting' });

    try {
      const { data, error } = await apiClient.POST('/auth/register', { body: request });

      if (data) {
        setState({ kind: 'registered', user: data });
        return;
      }

      setState({
        kind: 'failed',
        message: error?.detail ?? error?.title ?? unreachable,
        fieldErrors: error && 'errors' in error ? error.errors : {},
      });
    } catch (reason: unknown) {
      setState({
        kind: 'failed',
        message: reason instanceof Error ? reason.message : unreachable,
        fieldErrors: {},
      });
    }
  }, []);

  return { state, register };
}
