import { useState } from 'react';
import { MissingInputLinks, missingInputs } from './MissingInputs';
import { deleteVacancy } from './deleteVacancy';
import { formatVacancyDate } from './formatVacancyDate';
import { useVacancies, vacanciesPageSize } from './useVacancies';
import { vacancyHash } from './useVacancyView';

type VacancyListProps = {
  onPaste: () => void;
};

export function VacancyList({ onPaste }: VacancyListProps) {
  const [page, setPage] = useState(1);
  const [reloadKey, setReloadKey] = useState(0);
  const [confirmingId, setConfirmingId] = useState<string | null>(null);
  const [deleting, setDeleting] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);
  const state = useVacancies(page, reloadKey);
  const lastPage = state.kind === 'loaded' ? Math.max(1, Math.ceil(state.total / vacanciesPageSize)) : 1;

  async function confirmDelete(id: string) {
    setDeleting(true);
    const outcome = await deleteVacancy(id);
    setDeleting(false);
    setConfirmingId(null);
    setDeleteError(outcome.ok ? null : outcome.message);
    // Refresh even on failure: a 404 means it is already gone (e.g. deleted in another tab).
    if (state.kind === 'loaded' && state.items.length === 1 && page > 1) {
      setPage(page - 1);
    }
    setReloadKey((key) => key + 1);
  }

  return (
    <section
      aria-label="Vacancies"
      className="w-full max-w-2xl rounded-lg border border-slate-200 bg-white px-6 py-4 shadow-sm"
    >
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-semibold">My vacancies</h2>
        <a
          href="#/vacancies/new"
          onClick={(event) => {
            event.preventDefault();
            onPaste();
          }}
          className="rounded-md bg-slate-900 px-3 py-1.5 text-sm font-medium text-white hover:bg-slate-700"
        >
          Paste a vacancy
        </a>
      </div>
      {state.kind === 'loading' && <p data-testid="vacancies-loading">Loading your vacancies…</p>}
      {deleteError && (
        <p role="alert" data-testid="vacancy-delete-error" className="mt-2 text-sm text-red-600">
          {deleteError}
        </p>
      )}
      {state.kind === 'failed' && (
        <p data-testid="vacancies-error" className="text-red-600">
          {state.detail}
        </p>
      )}
      {state.kind === 'loaded' && state.total === 0 && (
        <p data-testid="vacancies-empty" className="mt-2 text-sm text-slate-600">
          You have no vacancies yet.{' '}
          <a
            href="#/vacancies/new"
            onClick={(event) => {
              event.preventDefault();
              onPaste();
            }}
            className="font-medium underline"
          >
            Paste your first vacancy
          </a>
        </p>
      )}
      {state.kind === 'loaded' && state.items.length > 0 && (
        <>
          {state.missingInputs.length > 0 && (
            <div data-testid="vacancies-missing" className="mt-2 rounded-md border border-amber-300 bg-amber-50 px-3 py-2 text-sm">
              <p>Vacancies cannot be scored yet. Missing: {state.missingInputs.map((input) => missingInputs[input].label).join(', ')}.</p>
              <MissingInputLinks inputs={state.missingInputs} testIdPrefix="vacancies-missing" />
            </div>
          )}
          <ul className="mt-2 divide-y divide-slate-200">
            {state.items.map((vacancy) => (
              <li key={vacancy.id} data-testid="vacancy-row" className="flex flex-col gap-1 py-2">
                <div className="flex items-baseline justify-between gap-4">
                  <div>
                    <p data-testid="vacancy-title" className="font-medium">
                      <a href={vacancyHash(vacancy.id)} className="hover:underline">
                        {vacancy.title}
                      </a>
                    </p>
                    <p data-testid="vacancy-company" className="text-sm text-slate-600">
                      {vacancy.company}
                    </p>
                  </div>
                  <div className="flex items-baseline gap-3">
                    {vacancy.score !== null && (
                      <span
                        data-testid="vacancy-row-score"
                        className={`text-lg font-semibold ${vacancy.failsMustHave ? 'text-red-700' : 'text-green-700'}`}
                      >
                        {vacancy.score}
                      </span>
                    )}
                    <time data-testid="vacancy-created" dateTime={vacancy.createdAt} className="text-xs text-slate-500">
                      {formatVacancyDate(vacancy.createdAt)}
                    </time>
                  </div>
                </div>
                {vacancy.failsMustHave && (
                  <p data-testid="vacancy-must-have-failed" className="text-sm font-medium text-red-700">
                    Fails must-have: {vacancy.unmetMustHaves.join(', ')}
                  </p>
                )}
                {vacancy.verdicts.length > 0 && (
                  <ul aria-label="Why this score" className="text-sm text-slate-600">
                    {vacancy.verdicts.map((verdict) => (
                      <li key={`${verdict.kind}:${verdict.requirement}`} data-testid="vacancy-row-explanation">
                        {verdict.explanation}
                      </li>
                    ))}
                  </ul>
                )}
                <div className="flex items-center gap-3">
                  {confirmingId === vacancy.id ? (
                    <span className="flex items-center gap-2 text-xs">
                      <span>Delete this vacancy?</span>
                      <button
                        type="button"
                        disabled={deleting}
                        onClick={() => void confirmDelete(vacancy.id)}
                        aria-label={`Confirm delete ${vacancy.title}`}
                        className="rounded-md bg-red-600 px-2 py-1 font-medium text-white hover:bg-red-700 disabled:opacity-50"
                      >
                        Confirm
                      </button>
                      <button
                        type="button"
                        disabled={deleting}
                        onClick={() => setConfirmingId(null)}
                        aria-label={`Cancel delete ${vacancy.title}`}
                        className="rounded-md border border-slate-300 px-2 py-1 font-medium hover:bg-slate-100 disabled:opacity-50"
                      >
                        Cancel
                      </button>
                    </span>
                  ) : (
                    <button
                      type="button"
                      onClick={() => {
                        setDeleteError(null);
                        setConfirmingId(vacancy.id);
                      }}
                      aria-label={`Delete ${vacancy.title}`}
                      className="rounded-md border border-slate-300 px-2 py-1 text-xs font-medium hover:bg-slate-100"
                    >
                      Delete
                    </button>
                  )}
                </div>
              </li>
            ))}
          </ul>
          {lastPage > 1 && (
            <nav aria-label="Vacancy pages" className="mt-2 flex items-center gap-2 text-sm">
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
