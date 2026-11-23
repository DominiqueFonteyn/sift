import { bureausHash } from '../navigation/routes';
import { useBureau } from './useBureau';

type BureauDetailPageProps = {
  id: string;
  onBack: () => void;
};

export function BureauDetailPage({ id, onBack }: BureauDetailPageProps) {
  const state = useBureau(id);

  return (
    <section aria-label="Bureau" className="flex flex-col gap-6">
      <div>
        <a
          href={bureausHash}
          data-testid="bureau-back"
          onClick={(event) => {
            event.preventDefault();
            onBack();
          }}
          className="inline-flex items-center text-sm font-medium text-slate-600 transition-colors hover:text-slate-900"
        >
          ← Back to my bureaus
        </a>
      </div>
      {state.kind === 'loading' && (
        <p data-testid="bureau-loading" className="text-sm text-slate-500">
          Loading the bureau…
        </p>
      )}
      {state.kind === 'failed' && (
        <p
          data-testid="bureau-error"
          className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700"
        >
          {state.detail}
        </p>
      )}
      {state.kind === 'not-found' && (
        <p
          data-testid="bureau-not-found"
          className="rounded-xl border border-slate-200 bg-white px-6 py-8 text-center text-sm text-slate-600 shadow-xs"
        >
          We could not find that bureau.
        </p>
      )}
      {state.kind === 'loaded' && (
        <article className="rounded-xl border border-slate-200 bg-white p-6 shadow-xs">
          <h2 data-testid="bureau-detail-name" className="text-2xl font-semibold tracking-tight text-slate-900">
            {state.bureau.name}
          </h2>
          {state.bureau.website && (
            <p data-testid="bureau-detail-website" className="mt-2 text-sm text-slate-600">
              <a href={state.bureau.website} target="_blank" rel="noreferrer" className="underline hover:text-slate-900">
                {state.bureau.website}
              </a>
            </p>
          )}
        </article>
      )}
    </section>
  );
}
