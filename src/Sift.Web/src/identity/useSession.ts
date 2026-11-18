import { useSyncExternalStore } from 'react';
import { getSessionState, subscribeToSession, type SessionState } from './sessionStore';

export function useSession(): SessionState {
  return useSyncExternalStore(subscribeToSession, getSessionState);
}
