import { useState, type FormEvent } from 'react';
import { useRequirements, type RequirementKind } from './useRequirements';

const kindLabels: Record<RequirementKind, string> = {
  'must-have': 'Must-have',
  'nice-to-have': 'Nice-to-have',
};

export function RequirementsEditor() {
  const { state, add, remove } = useRequirements();
  const [text, setText] = useState('');
  const [kind, setKind] = useState<RequirementKind>('must-have');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true);
    const outcome = await add(text, kind);
    setBusy(false);

    if (outcome.ok) {
      setText('');
      setError(null);
    } else {
      setError(outcome.message);
    }
  }

  async function onDelete(id: string) {
    const outcome = await remove(id);
    setError(outcome.ok ? null : outcome.message);
  }

  return (
    <section id="requirements" tabIndex={-1} aria-label="Requirements" className="flex flex-col gap-6">
      <header>
        <h2 className="text-2xl font-semibold tracking-tight text-slate-900">My requirements</h2>
        <p className="mt-1 text-sm text-slate-500">
          Define what a role must offer and what is a bonus. Unmet must-haves automatically cap a vacancy&apos;s score.
        </p>
      </header>

      <div className="rounded-xl border border-slate-200 bg-white p-6 shadow-xs">
        <form onSubmit={onSubmit} noValidate className="flex flex-wrap items-end gap-3">
          <div className="flex min-w-48 flex-1 flex-col gap-1">
            <label htmlFor="requirement-text" className="text-sm font-medium text-slate-700">
              Requirement
            </label>
            <input
              id="requirement-text"
              type="text"
              value={text}
              onChange={(event) => setText(event.target.value)}
              aria-invalid={error !== null}
              aria-describedby={error ? 'requirement-error' : undefined}
              placeholder="e.g. C#, Remote, €90/hr"
              className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm"
            />
          </div>
          <div className="flex flex-col gap-1">
            <label htmlFor="requirement-kind" className="text-sm font-medium text-slate-700">
              Kind
            </label>
            <select
              id="requirement-kind"
              value={kind}
              onChange={(event) => setKind(event.target.value as RequirementKind)}
              className="rounded-md border border-slate-300 px-3 py-2 text-sm"
            >
              <option value="must-have">{kindLabels['must-have']}</option>
              <option value="nice-to-have">{kindLabels['nice-to-have']}</option>
            </select>
          </div>
          <button
            type="submit"
            disabled={busy}
            className="rounded-lg bg-slate-900 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-slate-700 disabled:opacity-50"
          >
            Add requirement
          </button>
        </form>
        {error && (
          <p id="requirement-error" role="alert" data-testid="requirement-error" className="mt-2 text-sm text-red-600">
            {error}
          </p>
        )}
      </div>

      <div className="rounded-xl border border-slate-200 bg-white p-6 shadow-xs">
        {state.kind === 'loading' && (
          <p data-testid="requirements-loading" className="text-sm text-slate-500">
            Loading your requirements…
          </p>
        )}
        {state.kind === 'failed' && (
          <p data-testid="requirements-load-error" className="text-sm text-red-600">
            {state.detail}
          </p>
        )}
        {state.kind === 'ready' && state.items.length === 0 && (
          <p className="text-sm text-slate-500">No requirements yet. Add your first must-have or nice-to-have above.</p>
        )}
        {state.kind === 'ready' && state.items.length > 0 && (
          <ul className="flex flex-col divide-y divide-slate-100" aria-label="Requirement list">
            {state.items.map((item) => (
              <li key={item.id} data-testid="requirement-item" className="flex items-center gap-3 py-3 first:pt-0 last:pb-0">
                <span className="flex-1 text-sm font-medium text-slate-900">{item.text}</span>
                <span
                  data-testid="requirement-kind"
                  className={
                    item.kind === 'must-have'
                      ? 'rounded-full bg-red-100 px-2.5 py-0.5 text-xs font-medium text-red-800'
                      : 'rounded-full bg-sky-100 px-2.5 py-0.5 text-xs font-medium text-sky-800'
                  }
                >
                  {kindLabels[item.kind]}
                </span>
                <button
                  type="button"
                  onClick={() => void onDelete(item.id)}
                  aria-label={`Delete ${item.text}`}
                  className="rounded-md px-2.5 py-1 text-xs font-medium text-slate-500 transition-colors hover:bg-slate-100 hover:text-red-600"
                >
                  Delete
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>
    </section>
  );
}
