import { useHealth } from './health/useHealth';
import { LoginForm } from './identity/LoginForm';
import { RegisterForm } from './identity/RegisterForm';
import { SignedIn } from './identity/SignedIn';
import { useSession } from './identity/useSession';

export function App() {
  const health = useHealth();
  const { session, expired } = useSession();

  return (
    <main className="flex min-h-screen flex-col items-center justify-center gap-4 bg-slate-50 p-8 text-slate-900">
      <h1 className="text-3xl font-semibold tracking-tight">Sift</h1>
      <p className="text-sm text-slate-500">Walking skeleton</p>
      <section
        aria-label="API health"
        className="rounded-lg border border-slate-200 bg-white px-6 py-4 shadow-sm"
      >
        {health.kind === 'loading' && <p data-testid="health-status">Checking the API…</p>}
        {health.kind === 'loaded' && (
          <p data-testid="health-status">
            API status: <span className="font-semibold">{health.status}</span>
          </p>
        )}
        {health.kind === 'failed' && (
          <p data-testid="health-status" className="text-red-600">
            API unreachable: {health.detail}
          </p>
        )}
      </section>
      {session ? (
        <SignedIn />
      ) : (
        <>
          <LoginForm sessionExpired={expired} />
          <RegisterForm />
        </>
      )}
    </main>
  );
}
