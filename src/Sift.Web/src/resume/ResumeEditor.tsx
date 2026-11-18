import { useState, type FormEvent } from 'react';
import { resumeMaxLength } from './resumeRules';
import { useResume } from './useResume';

export function ResumeEditor() {
  const { state, save } = useResume();

  return (
    <section
      aria-label="Resume"
      className="w-full max-w-2xl rounded-lg border border-slate-200 bg-white px-6 py-4 shadow-sm"
    >
      <h2 className="text-lg font-semibold">My resume</h2>
      {state.kind === 'loading' && <p data-testid="resume-loading">Loading your resume…</p>}
      {state.kind === 'failed' && (
        <p data-testid="resume-load-error" className="text-red-600">
          {state.detail}
        </p>
      )}
      {state.kind === 'ready' && <ResumeForm initialText={state.text} updatedAt={state.updatedAt} save={save} />}
    </section>
  );
}

type ResumeFormProps = {
  initialText: string;
  updatedAt: string | null;
  save: (text: string) => Promise<{ ok: true } | { ok: false; message: string }>;
};

function ResumeForm({ initialText, updatedAt, save }: ResumeFormProps) {
  const [text, setText] = useState(initialText);
  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);
  const [saving, setSaving] = useState(false);

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSaving(true);
    setSaved(false);
    const outcome = await save(text);
    setSaving(false);

    if (outcome.ok) {
      setError(null);
      setSaved(true);
    } else {
      setError(outcome.message);
    }
  }

  return (
    <form onSubmit={onSubmit} noValidate className="mt-2 flex flex-col gap-2">
      <label htmlFor="resume-text" className="text-sm font-medium">
        Resume text
      </label>
      <textarea
        id="resume-text"
        value={text}
        onChange={(event) => {
          setText(event.target.value);
          setSaved(false);
        }}
        aria-invalid={error !== null}
        aria-describedby={error ? 'resume-error' : undefined}
        rows={16}
        className="w-full rounded-md border border-slate-300 p-2 font-mono text-sm"
      />
      <p className="text-xs text-slate-500">
        {text.length.toLocaleString('en-US')} / {resumeMaxLength.toLocaleString('en-US')} characters
      </p>
      {error && (
        <p id="resume-error" role="alert" data-testid="resume-error" className="text-sm text-red-600">
          {error}
        </p>
      )}
      {saved && (
        <p role="status" data-testid="resume-saved" className="text-sm text-green-700">
          Resume saved.
        </p>
      )}
      {updatedAt && !saved && (
        <p data-testid="resume-updated-at" className="text-xs text-slate-500">
          Last saved {new Date(updatedAt).toLocaleString()}
        </p>
      )}
      <button
        type="submit"
        disabled={saving}
        className="self-start rounded-md bg-slate-900 px-4 py-2 text-sm font-medium text-white hover:bg-slate-700 disabled:opacity-50"
      >
        Save resume
      </button>
    </form>
  );
}
