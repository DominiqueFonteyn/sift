import { useState, type FormEvent } from 'react';
import { useRegister, type FieldErrors } from './useRegister';

const inputClass =
  'mt-1 block w-full rounded-md border border-slate-300 px-3 py-2 text-sm shadow-sm focus:border-slate-500 focus:outline-none';

function FieldMessages({ id, messages }: { id: string; messages?: string[] }) {
  if (!messages || messages.length === 0) {
    return null;
  }

  return (
    <ul id={id} role="alert" className="mt-1 text-sm text-red-600">
      {messages.map((message) => (
        <li key={message}>{message}</li>
      ))}
    </ul>
  );
}

export function RegisterForm() {
  const { state, register } = useRegister();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');

  const fieldErrors: FieldErrors = state.kind === 'failed' ? state.fieldErrors : {};
  const submitting = state.kind === 'submitting';

  const onSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    await register({ email, password });
  };

  if (state.kind === 'registered') {
    return (
      <section
        aria-label="Registration"
        className="w-full max-w-sm rounded-lg border border-slate-200 bg-white px-6 py-4 shadow-sm"
      >
        <p data-testid="register-success" className="text-sm text-green-700">
          Account created for <span className="font-semibold">{state.user.email}</span>.
        </p>
      </section>
    );
  }

  return (
    <section
      aria-label="Registration"
      className="w-full max-w-sm rounded-lg border border-slate-200 bg-white px-6 py-4 shadow-sm"
    >
      <h2 className="text-lg font-semibold">Create an account</h2>
      <form noValidate onSubmit={onSubmit} className="mt-4 flex flex-col gap-4">
        <div>
          <label htmlFor="register-email" className="text-sm font-medium">
            Email
          </label>
          <input
            id="register-email"
            name="email"
            type="email"
            autoComplete="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            aria-invalid={fieldErrors.email ? true : undefined}
            aria-describedby={fieldErrors.email ? 'register-email-errors' : undefined}
            className={inputClass}
          />
          <FieldMessages id="register-email-errors" messages={fieldErrors.email} />
        </div>
        <div>
          <label htmlFor="register-password" className="text-sm font-medium">
            Password
          </label>
          <input
            id="register-password"
            name="password"
            type="password"
            autoComplete="new-password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            aria-invalid={fieldErrors.password ? true : undefined}
            aria-describedby={fieldErrors.password ? 'register-password-errors' : undefined}
            className={inputClass}
          />
          <FieldMessages id="register-password-errors" messages={fieldErrors.password} />
        </div>
        {state.kind === 'failed' && Object.keys(fieldErrors).length === 0 && (
          <p role="alert" data-testid="register-error" className="text-sm text-red-600">
            {state.message}
          </p>
        )}
        <button
          type="submit"
          disabled={submitting}
          className="rounded-md bg-slate-900 px-4 py-2 text-sm font-medium text-white hover:bg-slate-700 disabled:opacity-50"
        >
          {submitting ? 'Creating account…' : 'Register'}
        </button>
      </form>
    </section>
  );
}
