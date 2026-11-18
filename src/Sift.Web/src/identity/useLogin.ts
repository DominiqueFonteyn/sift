import { useCallback, useState } from 'react';
import { apiClient } from '../api/client';
import type { components } from '../api/generated/schema';
import { setSession } from './sessionStore';

type LoginRequest = components['schemas']['LoginRequest'];

export type LoginState =
  | { kind: 'idle' }
  | { kind: 'submitting' }
  | { kind: 'failed'; message: string };

const unreachable = 'The API could not be reached.';

export function useLogin(): { state: LoginState; login: (request: LoginRequest) => Promise<void> } {
  const [state, setState] = useState<LoginState>({ kind: 'idle' });

  const login = useCallback(async (request: LoginRequest) => {
    setState({ kind: 'submitting' });

    try {
      const { data, error } = await apiClient.POST('/auth/login', { body: request });

      if (data) {
        setSession({ accessToken: data.accessToken, expiresAt: data.expiresAt });
        return;
      }

      setState({ kind: 'failed', message: error?.detail ?? error?.title ?? unreachable });
    } catch (reason: unknown) {
      setState({ kind: 'failed', message: reason instanceof Error ? reason.message : unreachable });
    }
  }, []);

  return { state, login };
}
