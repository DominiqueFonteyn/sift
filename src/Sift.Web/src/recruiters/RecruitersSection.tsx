import { useState, type FormEvent } from 'react';
import { createRecruiter, type RecruiterField, type RecruiterFieldErrors } from './createRecruiter';
import { useBureauChoices } from './useBureauChoices';
import { recruitersPageSize, useRecruiters } from './useRecruiters';

const inputClass = 'w-full rounded-md border border-slate-300 p-2 text-sm aria-[invalid=true]:border-red-500';

const formFields: { field: Exclude<RecruiterField, 'bureauId'>; label: string; type: string }[] = [
  { field: 'name', label: 'Name', type: 'text' },
  { field: 'email', label: 'Email', type: 'text' },
  { field: 'phone', label: 'Phone', type: 'text' },
  { field: 'linkedInUrl', label: 'LinkedIn URL', type: 'text' },
];

const emptyDraft: Record<RecruiterField, string> = { name: '', email: '', phone: '', linkedInUrl: '', bureauId: '' };

export function RecruitersSection() {
  const [page, setPage] = useState(1);
  const [reloadKey, setReloadKey] = useState(0);
  const [draft, setDraft] = useState(emptyDraft);
  const [errors, setErrors] = useState<RecruiterFieldErrors>({});
  const [message, setMessage] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  // Bumped when the picker gets focus, so a bureau added moments ago is offered.
  const [choicesKey, setChoicesKey] = useState(0);
  const choices = useBureauChoices(choicesKey);
  const state = useRecruiters(page, reloadKey);
  const lastPage = state.kind === 'loaded' ? Math.max(1, Math.ceil(state.total / recruitersPageSize)) : 1;

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSaving(true);
    setMessage(null);
    const outcome = await createRecruiter(draft);
    setSaving(false);

    if (outcome.ok) {
      setDraft(emptyDraft);
      setErrors({});
      // The list is oldest first, so the new recruiter lands on the last page.
      const total = state.kind === 'loaded' ? state.total + 1 : 1;
      setPage(Math.max(1, Math.ceil(total / recruitersPageSize)));
      setReloadKey((key) => key + 1);
    } else {
      setErrors(outcome.fieldErrors);
      setMessage(outcome.message);
    }
  }

  return (
    <section aria-label="Recruiters" className="flex flex-col gap-6">
      <header>
        <h2 className="text-2xl font-semibold tracking-tight text-slate-900">My recruiters</h2>
        <p className="mt-1 text-sm text-slate-500">Keep track of the recruiters sending you roles.</p>
      </header>

      <div className="rounded-xl border border-slate-200 bg-white p-6 shadow-xs">
        <h3 className="text-sm font-semibold text-slate-900">Add a recruiter</h3>
        <form onSubmit={onSubmit} noValidate aria-label="Add a recruiter" className="mt-4 flex flex-col gap-4">
          <div className="grid gap-4 sm:grid-cols-2">
            {formFields.map(({ field, label, type }) => (
              <div key={field} className="flex flex-col gap-1">
                <label htmlFor={`recruiter-${field}`} className="text-sm font-medium text-slate-700">
                  {label}
                </label>
                <input
                  id={`recruiter-${field}`}
                  type={type}
                  value={draft[field]}
                  onChange={(event) => setDraft({ ...draft, [field]: event.target.value })}
                  aria-invalid={errors[field] !== undefined}
                  aria-describedby={errors[field] ? `recruiter-${field}-error` : undefined}
                  className={inputClass}
                />
                {errors[field] && (
                  <p id={`recruiter-${field}-error`} role="alert" data-testid={`recruiter-${field}-error`} className="text-sm text-red-600">
                    {errors[field]}
                  </p>
                )}
              </div>
            ))}
            <div className="flex flex-col gap-1 sm:col-span-2">
              <label htmlFor="recruiter-bureauId" className="text-sm font-medium text-slate-700">
                Bureau
              </label>
              <select
                id="recruiter-bureauId"
                value={draft.bureauId}
                onChange={(event) => setDraft({ ...draft, bureauId: event.target.value })}
                onFocus={() => setChoicesKey((key) => key + 1)}
                aria-invalid={errors.bureauId !== undefined}
                aria-describedby={errors.bureauId ? 'recruiter-bureauId-error' : undefined}
                className={inputClass}
              >
                <option value="">No bureau</option>
                {choices.kind === 'loaded' &&
                  choices.items.map((bureau) => (
                    <option key={bureau.id} value={bureau.id}>
                      {bureau.name}
                    </option>
                  ))}
              </select>
              {choices.kind === 'failed' && (
                <p data-testid="recruiter-bureau-choices-error" className="text-sm text-red-600">
                  {choices.detail}
                </p>
              )}
              {errors.bureauId && (
                <p id="recruiter-bureauId-error" role="alert" data-testid="recruiter-bureauId-error" className="text-sm text-red-600">
                  {errors.bureauId}
                </p>
              )}
            </div>
          </div>
          {message && (
            <p role="alert" data-testid="recruiter-error" className="text-sm text-red-600">
              {message}
            </p>
          )}
          <div>
            <button
              type="submit"
              disabled={saving}
              className="rounded-lg bg-slate-900 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-slate-700 disabled:opacity-50"
            >
              Add recruiter
            </button>
          </div>
        </form>
      </div>

      <div className="rounded-xl border border-slate-200 bg-white p-6 shadow-xs">
        {state.kind === 'loading' && (
          <p data-testid="recruiters-loading" className="text-sm text-slate-500">
            Loading your recruiters…
          </p>
        )}
        {state.kind === 'failed' && (
          <p data-testid="recruiters-error" className="text-sm text-red-600">
            {state.detail}
          </p>
        )}
        {state.kind === 'loaded' && state.total === 0 && (
          <p data-testid="recruiters-empty" className="text-sm text-slate-500">
            You have no recruiters yet.
          </p>
        )}
        {state.kind === 'loaded' && state.items.length > 0 && (
          <>
            <ul className="divide-y divide-slate-100">
              {state.items.map((recruiter) => (
                <li key={recruiter.id} data-testid="recruiter-row" className="py-3 first:pt-0 last:pb-0">
                  <div className="flex flex-wrap items-baseline justify-between gap-2">
                    <p data-testid="recruiter-name" className="font-medium text-slate-900">
                      {recruiter.name}
                    </p>
                    {recruiter.bureau && (
                      <p data-testid="recruiter-bureau" className="rounded-full bg-slate-100 px-2.5 py-0.5 text-xs font-medium text-slate-700">
                        {recruiter.bureau.name}
                      </p>
                    )}
                  </div>
                  <div className="mt-1 flex flex-wrap gap-x-4 gap-y-1 text-sm text-slate-600">
                    {recruiter.email && <p data-testid="recruiter-email">{recruiter.email}</p>}
                    {recruiter.phone && <p data-testid="recruiter-phone">{recruiter.phone}</p>}
                    {recruiter.linkedInUrl && (
                      <p data-testid="recruiter-linkedin">
                        <a href={recruiter.linkedInUrl} target="_blank" rel="noreferrer" className="underline hover:text-slate-900">
                          {recruiter.linkedInUrl}
                        </a>
                      </p>
                    )}
                  </div>
                </li>
              ))}
            </ul>
            {lastPage > 1 && (
              <nav aria-label="Recruiter pages" className="mt-4 flex items-center justify-between border-t border-slate-100 pt-4 text-sm">
                <button
                  type="button"
                  disabled={page <= 1}
                  onClick={() => setPage(page - 1)}
                  className="rounded-md border border-slate-300 bg-white px-3 py-1.5 font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-50"
                >
                  Previous
                </button>
                <span className="text-slate-600">
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
