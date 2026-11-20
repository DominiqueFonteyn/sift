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
    <section
      aria-label="Requirements"
      className="w-full max-w-2xl rounded-lg border border-slate-200 bg-white px-6 py-4 shadow-sm"
    >
      <h2 className="text-lg font-semibold">My requirements</h2>
      <form onSubmit={onSubmit} noValidate className="mt-2 flex flex-wrap items-end gap-2">
        <div className="flex min-w-48 flex-1 flex-col gap-1">
          <label htmlFor="requirement-text" className="text-sm font-medium">
            Requirement
          </label>
          <input
            id="requirement-text"
            type="text"
            value={text}
            onChange={(event) => setText(event.target.value)}
            aria-invalid={error !== null}
            aria-describedby={error ? 'requirement-error' : undefined}
            className="w-full rounded-md border border-slate-300 p-2 text-sm"
          />
        </div>
        <div className="flex flex-col gap-1">
          <label htmlFor="requirement-kind" className="text-sm font-medium">
            Kind
          </label>
          <select
            id="requirement-kind"
            value={kind}
            onChange={(event) => setKind(event.target.value as RequirementKind)}
            className="rounded-md border border-slate-300 p-2 text-sm"
          >
            <option value="must-have">{kindLabels['must-have']}</option>
            <option value="nice-to-have">{kindLabels['nice-to-have']}</option>
          </select>
        </div>
        <button
          type="submit"
          disabled={busy}
          className="rounded-md bg-slate-900 px-4 py-2 text-sm font-medium text-white hover:bg-slate-700 disabled:opacity-50"
        >
          Add requirement
        </button>
      </form>
      {error && (
        <p id="requirement-error" role="alert" data-testid="requirement-error" className="mt-2 text-sm text-red-600">
          {error}
        </p>
      )}
      {state.kind === 'loading' && <p data-testid="requirements-loading">Loading your requirements…</p>}
      {state.kind === 'failed' && (
        <p data-testid="requirements-load-error" className="text-red-600">
          {state.detail}
        </p>
      )}
      {state.kind === 'ready' && state.items.length === 0 && (
        <p className="mt-3 text-sm text-slate-500">No requirements yet.</p>
      )}
      {state.kind === 'ready' && state.items.length > 0 && (
        <ul className="mt-3 flex flex-col divide-y divide-slate-100" aria-label="Requirement list">
          {state.items.map((item) => (
            <li key={item.id} data-testid="requirement-item" className="flex items-center gap-3 py-2">
              <span className="flex-1 text-sm">{item.text}</span>
              <span
                data-testid="requirement-kind"
                className={
                  item.kind === 'must-have'
                    ? 'rounded-full bg-red-100 px-2 py-0.5 text-xs font-medium text-red-800'
                    : 'rounded-full bg-sky-100 px-2 py-0.5 text-xs font-medium text-sky-800'
                }
              >
                {kindLabels[item.kind]}
              </span>
              <button
                type="button"
                onClick={() => void onDelete(item.id)}
                aria-label={`Delete ${item.text}`}
                className="rounded-md border border-slate-300 px-3 py-1 text-xs font-medium hover:bg-slate-100"
              >
                Delete
              </button>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
