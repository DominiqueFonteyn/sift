import { useState, type FormEvent } from 'react';
import { BureauDetailPage } from './BureauDetailPage';
import { createBureau, type BureauField, type BureauFieldErrors } from './createBureau';
import { bureauHash, useBureauView } from './useBureauView';
import { bureausPageSize, useBureaus } from './useBureaus';

const inputClass = 'w-full rounded-md border border-slate-300 p-2 text-sm aria-[invalid=true]:border-red-500';

const formFields: { field: BureauField; label: string }[] = [
  { field: 'name', label: 'Name' },
  { field: 'website', label: 'Website' },
];

const emptyDraft: Record<BureauField, string> = { name: '', website: '' };

export function BureausSection() {
  const { view, show } = useBureauView();
  const [page, setPage] = useState(1);
  const [reloadKey, setReloadKey] = useState(0);
  const [draft, setDraft] = useState(emptyDraft);
  const [errors, setErrors] = useState<BureauFieldErrors>({});
  const [message, setMessage] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const state = useBureaus(page, reloadKey);
  const lastPage = state.kind === 'loaded' ? Math.max(1, Math.ceil(state.total / bureausPageSize)) : 1;

  if (view.kind === 'detail') {
    return <BureauDetailPage id={view.id} onBack={() => show({ kind: 'list' })} />;
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
    <section
      aria-label="Bureaus"
      className="w-full max-w-2xl rounded-lg border border-slate-200 bg-white px-6 py-4 shadow-sm"
    >
      <h2 className="text-lg font-semibold">My bureaus</h2>
      <form onSubmit={onSubmit} noValidate aria-label="Add a bureau" className="mt-2 flex flex-col gap-2">
        {formFields.map(({ field, label }) => (
          <div key={field} className="flex flex-col gap-1">
            <label htmlFor={`bureau-${field}`} className="text-sm font-medium">
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
        {message && (
          <p role="alert" data-testid="bureau-form-error" className="text-sm text-red-600">
            {message}
          </p>
        )}
        <div>
          <button
            type="submit"
            disabled={saving}
            className="rounded-md bg-slate-900 px-4 py-2 text-sm font-medium text-white hover:bg-slate-700 disabled:opacity-50"
          >
            Add bureau
          </button>
        </div>
      </form>

      {state.kind === 'loading' && <p data-testid="bureaus-loading">Loading your bureaus…</p>}
      {state.kind === 'failed' && (
        <p data-testid="bureaus-error" className="text-red-600">
          {state.detail}
        </p>
      )}
      {state.kind === 'loaded' && state.total === 0 && (
        <p data-testid="bureaus-empty" className="mt-4 text-sm text-slate-600">
          You have no bureaus yet.
        </p>
      )}
      {state.kind === 'loaded' && state.items.length > 0 && (
        <>
          <ul className="mt-4 divide-y divide-slate-200">
            {state.items.map((bureau) => (
              <li key={bureau.id} data-testid="bureau-row" className="py-2">
                <a href={bureauHash(bureau.id)} data-testid="bureau-name" className="font-medium underline">
                  {bureau.name}
                </a>
                {bureau.website && (
                  <p data-testid="bureau-website" className="text-sm text-slate-600">
                    {bureau.website}
                  </p>
                )}
              </li>
            ))}
          </ul>
          {lastPage > 1 && (
            <nav aria-label="Bureau pages" className="mt-2 flex items-center gap-2 text-sm">
              <button
                type="button"
                disabled={page <= 1}
                onClick={() => setPage(page - 1)}
                className="rounded-md border border-slate-300 px-3 py-1 hover:bg-slate-100 disabled:opacity-50"
              >
                Previous
              </button>
              <span data-testid="bureau-page-status">
                Page {page} of {lastPage}
              </span>
              <button
                type="button"
                disabled={page >= lastPage}
                onClick={() => setPage(page + 1)}
                className="rounded-md border border-slate-300 px-3 py-1 hover:bg-slate-100 disabled:opacity-50"
              >
                Next
              </button>
            </nav>
          )}
        </>
      )}
    </section>
  );
}
