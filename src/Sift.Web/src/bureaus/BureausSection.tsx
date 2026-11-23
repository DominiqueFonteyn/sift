import { useState, type FormEvent } from 'react';
import { bureauHash, bureausHash, type BureauView } from '../navigation/routes';
import { BureauDetailPage } from './BureauDetailPage';
import { createBureau, type BureauField, type BureauFieldErrors } from './createBureau';
import { bureausPageSize, useBureaus } from './useBureaus';

const inputClass = 'w-full rounded-md border border-slate-300 p-2 text-sm aria-[invalid=true]:border-red-500';

const formFields: { field: BureauField; label: string }[] = [
  { field: 'name', label: 'Name' },
  { field: 'website', label: 'Website' },
];

const emptyDraft: Record<BureauField, string> = { name: '', website: '' };

type BureausSectionProps = {
  view: BureauView;
  onNavigate: (hash: string) => void;
};

export function BureausSection({ view, onNavigate }: BureausSectionProps) {
  const [page, setPage] = useState(1);
  const [reloadKey, setReloadKey] = useState(0);
  const [draft, setDraft] = useState(emptyDraft);
  const [errors, setErrors] = useState<BureauFieldErrors>({});
  const [message, setMessage] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const state = useBureaus(page, reloadKey);
  const lastPage = state.kind === 'loaded' ? Math.max(1, Math.ceil(state.total / bureausPageSize)) : 1;

  if (view.kind === 'detail') {
    return <BureauDetailPage id={view.id} onBack={() => onNavigate(bureausHash)} />;
  }

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSaving(true);
    setMessage(null);
    // An empty website is simply left out, so the API stores no website.
    const outcome = await createBureau({ name: draft.name, ...(draft.website.trim() ? { website: draft.website } : {}) });
    setSaving(false);

    if (outcome.ok) {
      setDraft(emptyDraft);
      setErrors({});
      // The list is oldest first, so the new bureau lands on the last page.
      const total = state.kind === 'loaded' ? state.total + 1 : 1;
      setPage(Math.max(1, Math.ceil(total / bureausPageSize)));
      setReloadKey((key) => key + 1);
    } else {
      setErrors(outcome.fieldErrors);
      setMessage(outcome.message);
    }
  }

  return (
    <section aria-label="Bureaus" className="flex flex-col gap-6">
      <header>
        <h2 className="text-2xl font-semibold tracking-tight text-slate-900">My bureaus</h2>
        <p className="mt-1 text-sm text-slate-500">Organize the recruitment agencies you work with.</p>
      </header>

      <div className="rounded-xl border border-slate-200 bg-white p-6 shadow-xs">
        <h3 className="text-sm font-semibold text-slate-900">Add a bureau</h3>
        <form onSubmit={onSubmit} noValidate aria-label="Add a bureau" className="mt-4 flex flex-col gap-4">
          <div className="grid gap-4 sm:grid-cols-2">
            {formFields.map(({ field, label }) => (
              <div key={field} className="flex flex-col gap-1">
                <label htmlFor={`bureau-${field}`} className="text-sm font-medium text-slate-700">
                  {label}
                </label>
                <input
                  id={`bureau-${field}`}
                  type="text"
                  value={draft[field]}
                  onChange={(event) => setDraft({ ...draft, [field]: event.target.value })}
                  aria-invalid={errors[field] !== undefined}
                  aria-describedby={errors[field] ? `bureau-${field}-error` : undefined}
                  className={inputClass}
                />
                {errors[field] && (
                  <p id={`bureau-${field}-error`} role="alert" data-testid={`bureau-${field}-error`} className="text-sm text-red-600">
                    {errors[field]}
                  </p>
                )}
              </div>
            ))}
          </div>
          {message && (
            <p role="alert" data-testid="bureau-form-error" className="text-sm text-red-600">
              {message}
            </p>
          )}
          <div>
            <button
              type="submit"
              disabled={saving}
              className="rounded-lg bg-slate-900 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-slate-700 disabled:opacity-50"
            >
              Add bureau
            </button>
          </div>
        </form>
      </div>

      <div className="rounded-xl border border-slate-200 bg-white p-6 shadow-xs">
        {state.kind === 'loading' && (
          <p data-testid="bureaus-loading" className="text-sm text-slate-500">
            Loading your bureaus…
          </p>
        )}
        {state.kind === 'failed' && (
          <p data-testid="bureaus-error" className="text-sm text-red-600">
            {state.detail}
          </p>
        )}
        {state.kind === 'loaded' && state.total === 0 && (
          <p data-testid="bureaus-empty" className="text-sm text-slate-500">
            You have no bureaus yet.
          </p>
        )}
        {state.kind === 'loaded' && state.items.length > 0 && (
          <>
            <ul className="divide-y divide-slate-100">
              {state.items.map((bureau) => (
                <li key={bureau.id} data-testid="bureau-row" className="py-3 first:pt-0 last:pb-0">
                  <a href={bureauHash(bureau.id)} data-testid="bureau-name" className="font-medium text-slate-900 hover:underline">
                    {bureau.name}
                  </a>
                  {bureau.website && (
                    <p data-testid="bureau-website" className="mt-0.5 text-sm text-slate-500">
                      {bureau.website}
                    </p>
                  )}
                </li>
              ))}
            </ul>
            {lastPage > 1 && (
              <nav aria-label="Bureau pages" className="mt-4 flex items-center justify-between border-t border-slate-100 pt-4 text-sm">
                <button
                  type="button"
                  disabled={page <= 1}
                  onClick={() => setPage(page - 1)}
                  className="rounded-md border border-slate-300 bg-white px-3 py-1.5 font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-50"
                >
                  Previous
                </button>
                <span data-testid="bureau-page-status" className="text-slate-600">
                  Page {page} of {lastPage}
                </span>
                <button
                  type="button"
                  disabled={page >= lastPage}
                  onClick={() => setPage(page + 1)}
                  className="rounded-md border border-slate-300 bg-white px-3 py-1.5 font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-50"
                >
                  Next
                </button>
              </nav>
            )}
          </>
        )}
      </div>
    </section>
  );
}
