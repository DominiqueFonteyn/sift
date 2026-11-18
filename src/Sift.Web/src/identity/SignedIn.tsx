import { clearSession } from './sessionStore';
import { useCurrentUser } from './useCurrentUser';

export function SignedIn() {
  const current = useCurrentUser();

  return (
    <section
      aria-label="Account"
      className="w-full max-w-sm rounded-lg border border-slate-200 bg-white px-6 py-4 shadow-sm"
    >
      <p data-testid="signed-in" className="text-sm">
        {current.kind === 'loading' && 'Signed in'}
        {current.kind === 'loaded' && (
          <>
            Signed in as <span className="font-semibold">{current.user.email}</span>
          </>
        )}
        {current.kind === 'failed' && <span className="text-red-600">{current.detail}</span>}
      </p>
      <button
        type="button"
        onClick={clearSession}
        className="mt-4 rounded-md border border-slate-300 px-4 py-2 text-sm font-medium hover:bg-slate-100"
      >
        Log out
      </button>
    </section>
  );
}
