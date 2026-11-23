import { useEffect, useState } from 'react';
import { HealthBadge } from '../health/HealthBadge';
import type { HealthState } from '../health/useHealth';
import { LoginForm } from './LoginForm';
import { RegisterForm } from './RegisterForm';

type AuthMode = 'login' | 'register';

type AuthScreenProps = {
  sessionExpired: boolean;
  health: HealthState;
};

export function AuthScreen({ sessionExpired, health }: AuthScreenProps) {
  const [mode, setMode] = useState<AuthMode>('login');

  useEffect(() => {
    if (sessionExpired) {
      setMode('login');
    }
  }, [sessionExpired]);

  return (
    <main className="flex min-h-screen flex-col items-center justify-center bg-slate-50 px-4 py-12 text-slate-900">
      <div className="w-full max-w-md">
        <header className="mb-6 text-center">
          <h1 className="text-3xl font-semibold tracking-tight text-slate-900">Sift</h1>
          <p className="mt-1 text-sm text-slate-500">Find the vacancies worth your time.</p>
        </header>

        <div className="mb-4 grid grid-cols-2 rounded-lg border border-slate-200 bg-slate-100 p-1">
          <button
            type="button"
            onClick={() => setMode('login')}
            aria-pressed={mode === 'login'}
            className={
              mode === 'login'
                ? 'rounded-md bg-white px-3 py-1.5 text-sm font-medium text-slate-900 shadow-xs'
                : 'rounded-md px-3 py-1.5 text-sm font-medium text-slate-600 hover:text-slate-900'
            }
          >
            Log in
          </button>
          <button
            type="button"
            onClick={() => setMode('register')}
            aria-pressed={mode === 'register'}
            className={
              mode === 'register'
                ? 'rounded-md bg-white px-3 py-1.5 text-sm font-medium text-slate-900 shadow-xs'
                : 'rounded-md px-3 py-1.5 text-sm font-medium text-slate-600 hover:text-slate-900'
            }
          >
            Create an account
          </button>
        </div>

        {mode === 'login' ? (
          <LoginForm sessionExpired={sessionExpired} />
        ) : (
          <RegisterForm onSwitchToLogin={() => setMode('login')} />
        )}

        <div className="mt-6 flex justify-center">
          <HealthBadge health={health} />
        </div>
      </div>
    </main>
  );
}
