import { clearSession } from './sessionStore';
import { useCurrentUser } from './useCurrentUser';

export function SignedIn() {
  const current = useCurrentUser();

  return (
    <section aria-label="Account" className="flex flex-col gap-2">
      <p data-testid="signed-in" className="truncate text-xs text-slate-600">
        {current.kind === 'loading' && 'Signed in'}
        {current.kind === 'loaded' && (
          <>
            Signed in as <span className="font-medium text-slate-900">{current.user.email}</span>
          </>
        )}
        {current.kind === 'failed' && <span className="text-red-600">{current.detail}</span>}
      </p>
      <button
        type="button"
        onClick={clearSession}
        className="w-full rounded-md border border-slate-200 bg-white px-3 py-1.5 text-xs font-medium text-slate-700 transition-colors hover:bg-slate-100"
      >
        Log out
      </button>
    </section>
  );
}
