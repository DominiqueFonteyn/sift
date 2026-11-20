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
    <section
      aria-label="New vacancy"
      className="w-full max-w-2xl rounded-lg border border-slate-200 bg-white px-6 py-4 shadow-sm"
    >
      <h2 className="text-lg font-semibold">Paste a vacancy</h2>
      <form onSubmit={onSubmit} noValidate className="mt-2 flex flex-col gap-2">
        <label htmlFor="vacancy-title" className="text-sm font-medium">
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

        <label htmlFor="vacancy-company" className="text-sm font-medium">
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

        <label htmlFor="vacancy-text" className="text-sm font-medium">
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

        {message && (
          <p role="alert" data-testid="vacancy-error" className="text-sm text-red-600">
            {message}
          </p>
        )}
        <div className="flex gap-2">
          <button
            type="submit"
            disabled={saving}
            className="rounded-md bg-slate-900 px-4 py-2 text-sm font-medium text-white hover:bg-slate-700 disabled:opacity-50"
          >
            Save vacancy
          </button>
          <button
            type="button"
            onClick={onCancel}
            className="rounded-md border border-slate-300 px-4 py-2 text-sm font-medium hover:bg-slate-100"
          >
            Cancel
          </button>
        </div>
      </form>
    </section>
  );
}
