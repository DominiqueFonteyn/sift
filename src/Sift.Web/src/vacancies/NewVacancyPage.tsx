import { useState, type FormEvent } from 'react';
import { companyMaxLength, rawTextMaxLength, titleMaxLength, type VacancyField, type VacancyFieldErrors } from './vacancyRules';
import { useCreateVacancy } from './useCreateVacancy';

type NewVacancyPageProps = {
  onSaved: () => void;
  onCancel: () => void;
};

const inputClass = 'w-full rounded-md border border-slate-300 p-2 text-sm aria-[invalid=true]:border-red-500';

export function NewVacancyPage({ onSaved, onCancel }: NewVacancyPageProps) {
  const create = useCreateVacancy();
  const [title, setTitle] = useState('');
  const [company, setCompany] = useState('');
  const [rawText, setRawText] = useState('');
  const [errors, setErrors] = useState<VacancyFieldErrors>({});
  const [message, setMessage] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSaving(true);
    setMessage(null);
    const outcome = await create({ title, company, rawText });
    setSaving(false);

    if (outcome.ok) {
      onSaved();
    } else {
      setErrors(outcome.fieldErrors);
      setMessage(outcome.message);
    }
  }

  function fieldError(field: VacancyField) {
    const text = errors[field];

    return text ? (
      <p id={`vacancy-${field}-error`} role="alert" data-testid={`vacancy-${field}-error`} className="text-sm text-red-600">
        {text}
      </p>
    ) : null;
  }

  function describedBy(field: VacancyField) {
    return errors[field] ? `vacancy-${field}-error` : undefined;
  }

  return (
    <section aria-label="New vacancy" className="flex flex-col gap-6">
      <header>
        <h2 className="text-2xl font-semibold tracking-tight text-slate-900">Paste a vacancy</h2>
        <p className="mt-1 text-sm text-slate-500">
          Drop in a job description to score it immediately against your requirements.
        </p>
      </header>

      <div className="rounded-xl border border-slate-200 bg-white p-6 shadow-xs">
        <form onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="flex flex-col gap-1">
              <label htmlFor="vacancy-title" className="text-sm font-medium text-slate-700">
                Title
              </label>
              <input
                id="vacancy-title"
                value={title}
                onChange={(event) => setTitle(event.target.value)}
                aria-invalid={errors.title !== undefined}
                aria-describedby={describedBy('title')}
                className={inputClass}
              />
              {fieldError('title')}
              <p className="text-xs text-slate-500">Up to {titleMaxLength} characters.</p>
            </div>

            <div className="flex flex-col gap-1">
              <label htmlFor="vacancy-company" className="text-sm font-medium text-slate-700">
                Company
              </label>
              <input
                id="vacancy-company"
                value={company}
                onChange={(event) => setCompany(event.target.value)}
                aria-invalid={errors.company !== undefined}
                aria-describedby={describedBy('company')}
                className={inputClass}
              />
              {fieldError('company')}
              <p className="text-xs text-slate-500">Up to {companyMaxLength} characters.</p>
            </div>
          </div>

          <div className="flex flex-col gap-1">
            <label htmlFor="vacancy-text" className="text-sm font-medium text-slate-700">
              Vacancy text
            </label>
            <textarea
              id="vacancy-text"
              value={rawText}
              onChange={(event) => setRawText(event.target.value)}
              aria-invalid={errors.rawText !== undefined}
              aria-describedby={describedBy('rawText')}
              rows={14}
              className={`${inputClass} font-mono`}
            />
            {fieldError('rawText')}
            <p className="text-xs text-slate-500">
              {rawText.length.toLocaleString('en-US')} / {rawTextMaxLength.toLocaleString('en-US')} characters
            </p>
          </div>

          {message && (
            <p role="alert" data-testid="vacancy-error" className="text-sm text-red-600">
              {message}
            </p>
          )}
          <div className="flex gap-2 pt-2">
            <button
              type="submit"
              disabled={saving}
              className="rounded-lg bg-slate-900 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-slate-700 disabled:opacity-50"
            >
              Save vacancy
            </button>
            <button
              type="button"
              onClick={onCancel}
              className="rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-100"
            >
              Cancel
            </button>
          </div>
        </form>
      </div>
    </section>
  );
}
