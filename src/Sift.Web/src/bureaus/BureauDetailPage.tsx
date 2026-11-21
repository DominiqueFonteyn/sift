import { useBureau } from './useBureau';

type BureauDetailPageProps = {
  id: string;
  onBack: () => void;
};

export function BureauDetailPage({ id, onBack }: BureauDetailPageProps) {
  const state = useBureau(id);

  return (
    <section aria-label="Bureau" className="flex w-full max-w-2xl flex-col gap-4">
      <a
        href="#/bureaus"
        data-testid="bureau-back"
        onClick={(event) => {
          event.preventDefault();
          onBack();
        }}
        className="text-sm font-medium underline"
      >
        Back to my bureaus
      </a>
      {state.kind === 'loading' && <p data-testid="bureau-loading">Loading the bureau…</p>}
      {state.kind === 'failed' && (
        <p data-testid="bureau-error" className="text-red-600">
          {state.detail}
        </p>
      )}
      {state.kind === 'not-found' && (
        <p data-testid="bureau-not-found" className="rounded-lg border border-slate-200 bg-white px-6 py-4 shadow-sm">
          We could not find that bureau.
        </p>
      )}
      {state.kind === 'loaded' && (
        <article className="rounded-lg border border-slate-200 bg-white px-6 py-4 shadow-sm">
          <h2 data-testid="bureau-detail-name" className="text-lg font-semibold">
            {state.bureau.name}
          </h2>
          {state.bureau.website && (
            <p data-testid="bureau-detail-website" className="text-sm text-slate-600">
              <a href={state.bureau.website} target="_blank" rel="noreferrer" className="underline">
                {state.bureau.website}
              </a>
            </p>
          )}
        </article>
      )}
    </section>
  );
}
