import type { components } from '../api/generated/schema';

type LoginResponse = components['schemas']['LoginResponse'];

export type Session = Pick<LoginResponse, 'accessToken' | 'expiresAt'>;

export type SessionState = { session: Session | null; expired: boolean };

const storageKey = 'sift.session';
// setTimeout overflows (and fires immediately) beyond a signed 32-bit delay.
const maxTimerDelay = 2_147_483_647;

const listeners = new Set<() => void>();
let timer: ReturnType<typeof setTimeout> | undefined;

function isExpired(session: Session): boolean {
  return Date.parse(session.expiresAt) <= Date.now();
}

function readStored(): Session | null {
  try {
    const raw = localStorage.getItem(storageKey);
    if (!raw) {
      return null;
    }

    const parsed: unknown = JSON.parse(raw);
    if (
      typeof parsed === 'object' &&
      parsed !== null &&
      'accessToken' in parsed &&
      'expiresAt' in parsed &&
      typeof parsed.accessToken === 'string' &&
      typeof parsed.expiresAt === 'string' &&
      !Number.isNaN(Date.parse(parsed.expiresAt))
    ) {
      return { accessToken: parsed.accessToken, expiresAt: parsed.expiresAt };
    }
  } catch {
    // A corrupt entry is treated as signed out.
  }

  return null;
}

let state: SessionState = { session: null, expired: false };

function publish(next: SessionState): void {
  state = next;
  listeners.forEach((listener) => listener());
}

function persist(session: Session | null): void {
  try {
    if (session) {
      localStorage.setItem(storageKey, JSON.stringify(session));
    } else {
      localStorage.removeItem(storageKey);
    }
  } catch {
    // Storage can be unavailable (private mode); the in-memory session still works.
  }
}

function scheduleExpiry(session: Session): void {
  clearTimeout(timer);
  const delay = Date.parse(session.expiresAt) - Date.now();
  timer = setTimeout(expireSession, Math.min(Math.max(delay, 0), maxTimerDelay));
}

export function setSession(session: Session): void {
  persist(session);
  scheduleExpiry(session);
  publish({ session, expired: false });
}

export function clearSession(): void {
  clearTimeout(timer);
  persist(null);
  publish({ session: null, expired: false });
}

// Ends the session because the token ran out or the API rejected it, so the login screen can say so.
export function expireSession(): void {
  clearTimeout(timer);
  persist(null);
  publish({ session: null, expired: state.session !== null });
}

export function getSessionState(): SessionState {
  return state;
}

export function subscribeToSession(listener: () => void): () => void {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
}

function initialise(): void {
  const stored = readStored();

  if (stored && !isExpired(stored)) {
    state = { session: stored, expired: false };
    scheduleExpiry(stored);
    return;
  }

  if (stored) {
    persist(null);
    state = { session: null, expired: true };
  }
}

initialise();
