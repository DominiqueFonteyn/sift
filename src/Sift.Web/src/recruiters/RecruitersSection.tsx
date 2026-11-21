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
    <section
      aria-label="Recruiters"
      className="w-full max-w-2xl rounded-lg border border-slate-200 bg-white px-6 py-4 shadow-sm"
    >
      <h2 className="text-lg font-semibold">My recruiters</h2>
      <form onSubmit={onSubmit} noValidate aria-label="Add a recruiter" className="mt-2 flex flex-col gap-2">
        {formFields.map(({ field, label, type }) => (
          <div key={field} className="flex flex-col gap-1">
            <label htmlFor={`recruiter-${field}`} className="text-sm font-medium">
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
        <div className="flex flex-col gap-1">
          <label htmlFor="recruiter-bureauId" className="text-sm font-medium">
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
        {message && (
          <p role="alert" data-testid="recruiter-error" className="text-sm text-red-600">
            {message}
          </p>
        )}
        <div>
          <button
            type="submit"
            disabled={saving}
            className="rounded-md bg-slate-900 px-4 py-2 text-sm font-medium text-white hover:bg-slate-700 disabled:opacity-50"
          >
            Add recruiter
          </button>
        </div>
      </form>

      {state.kind === 'loading' && <p data-testid="recruiters-loading">Loading your recruiters…</p>}
      {state.kind === 'failed' && (
        <p data-testid="recruiters-error" className="text-red-600">
          {state.detail}
        </p>
      )}
      {state.kind === 'loaded' && state.total === 0 && (
        <p data-testid="recruiters-empty" className="mt-4 text-sm text-slate-600">
          You have no recruiters yet.
        </p>
      )}
      {state.kind === 'loaded' && state.items.length > 0 && (
        <>
          <ul className="mt-4 divide-y divide-slate-200">
            {state.items.map((recruiter) => (
              <li key={recruiter.id} data-testid="recruiter-row" className="py-2">
                <p data-testid="recruiter-name" className="font-medium">
                  {recruiter.name}
                </p>
                {recruiter.bureau && (
                  <p data-testid="recruiter-bureau" className="text-sm text-slate-600">
                    {recruiter.bureau.name}
                  </p>
                )}
                {recruiter.email && (
                  <p data-testid="recruiter-email" className="text-sm text-slate-600">
                    {recruiter.email}
                  </p>
                )}
                {recruiter.phone && (
                  <p data-testid="recruiter-phone" className="text-sm text-slate-600">
                    {recruiter.phone}
                  </p>
                )}
                {recruiter.linkedInUrl && (
                  <p data-testid="recruiter-linkedin" className="text-sm text-slate-600">
                    <a href={recruiter.linkedInUrl} target="_blank" rel="noreferrer" className="underline">
                      {recruiter.linkedInUrl}
                    </a>
                  </p>
                )}
              </li>
            ))}
          </ul>
          {lastPage > 1 && (
            <nav aria-label="Recruiter pages" className="mt-2 flex items-center gap-2 text-sm">
              <button
                type="button"
                disabled={page <= 1}
                onClick={() => setPage(page - 1)}
                className="rounded-md border border-slate-300 px-3 py-1 hover:bg-slate-100 disabled:opacity-50"
              >
                Previous
              </button>
              <span>
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
