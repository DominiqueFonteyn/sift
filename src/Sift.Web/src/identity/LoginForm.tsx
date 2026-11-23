import { useState, type FormEvent } from 'react';
import { useLogin } from './useLogin';

const inputClass =
  'mt-1 block w-full rounded-md border border-slate-300 px-3 py-2 text-sm shadow-sm focus:border-slate-500 focus:outline-none';

export function LoginForm({ sessionExpired }: { sessionExpired: boolean }) {
  const { state, login } = useLogin();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');

  const submitting = state.kind === 'submitting';

  const onSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    await login({ email, password });
  };

  return (
    <section
      aria-label="Login"
      className="w-full rounded-xl border border-slate-200 bg-white p-6 shadow-xs"
    >
      <h2 className="text-lg font-semibold">Log in</h2>
      {sessionExpired && (
        <p role="status" data-testid="session-expired" className="mt-2 text-sm text-amber-700">
          Your session has ended. Please log in again.
        </p>
      )}
      <form noValidate onSubmit={onSubmit} className="mt-4 flex flex-col gap-4">
        <div>
          <label htmlFor="login-email" className="text-sm font-medium">
            Email
          </label>
          <input
            id="login-email"
            name="email"
            type="email"
            autoComplete="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            className={inputClass}
          />
        </div>
        <div>
          <label htmlFor="login-password" className="text-sm font-medium">
            Password
          </label>
          <input
            id="login-password"
            name="password"
            type="password"
            autoComplete="current-password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            className={inputClass}
          />
        </div>
        {state.kind === 'failed' && (
          <p role="alert" data-testid="login-error" className="text-sm text-red-600">
            {state.message}
          </p>
        )}
        <button
          type="submit"
          disabled={submitting}
          className="rounded-md bg-slate-900 px-4 py-2 text-sm font-medium text-white hover:bg-slate-700 disabled:opacity-50"
        >
          {submitting ? 'Logging in…' : 'Log in'}
        </button>
      </form>
    </section>
  );
}
