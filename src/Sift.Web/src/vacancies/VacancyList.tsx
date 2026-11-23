import { useState } from 'react';
import { newVacancyHash, vacancyHash } from '../navigation/routes';
import { MissingInputLinks, missingInputs } from './MissingInputs';
import { deleteVacancy } from './deleteVacancy';
import { formatVacancyDate } from './formatVacancyDate';
import { useVacancies, vacanciesPageSize } from './useVacancies';

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
    <section aria-label="Vacancies" className="flex flex-col gap-6">
      <header className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h2 className="text-2xl font-semibold tracking-tight text-slate-900">My vacancies</h2>
          <p className="mt-1 text-sm text-slate-500">
            Ranked by how well each role matches your must-haves and nice-to-haves.
          </p>
        </div>
        <a
          href={newVacancyHash}
          onClick={(event) => {
            event.preventDefault();
            onPaste();
          }}
          className="inline-flex items-center rounded-lg bg-slate-900 px-4 py-2 text-sm font-medium text-white shadow-xs transition-colors hover:bg-slate-700"
        >
          Paste a vacancy
        </a>
      </header>

      {state.kind === 'loading' && (
        <p data-testid="vacancies-loading" className="text-sm text-slate-500">
          Loading your vacancies…
        </p>
      )}

      {deleteError && (
        <p
          role="alert"
          data-testid="vacancy-delete-error"
          className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700"
        >
          {deleteError}
        </p>
      )}

      {state.kind === 'failed' && (
        <p
          data-testid="vacancies-error"
          className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700"
        >
          {state.detail}
        </p>
      )}

      {state.kind === 'loaded' && state.total === 0 && (
        <div
          data-testid="vacancies-empty"
          className="rounded-xl border border-dashed border-slate-300 bg-white px-6 py-12 text-center shadow-xs"
        >
          <p className="text-base font-medium text-slate-900">You have no vacancies yet.</p>
          <p className="mt-1 text-sm text-slate-500">
            Paste a job description to see how well it fits your profile.
          </p>
          <a
            href={newVacancyHash}
            onClick={(event) => {
              event.preventDefault();
              onPaste();
            }}
            className="mt-4 inline-flex items-center rounded-lg bg-slate-900 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-slate-700"
          >
            Paste your first vacancy
          </a>
        </div>
      )}

      {state.kind === 'loaded' && state.items.length > 0 && (
        <>
          {state.missingInputs.length > 0 && (
            <div
              data-testid="vacancies-missing"
              className="rounded-xl border border-amber-200 bg-amber-50 px-5 py-4 text-sm text-amber-950"
            >
              <p className="font-medium">
                Vacancies cannot be scored yet. Missing:{' '}
                {state.missingInputs.map((input) => missingInputs[input].label).join(', ')}.
              </p>
              <MissingInputLinks inputs={state.missingInputs} testIdPrefix="vacancies-missing" />
            </div>
          )}

          <ul className="flex flex-col gap-3">
            {state.items.map((vacancy) => (
              <li
                key={vacancy.id}
                data-testid="vacancy-row"
                className="rounded-xl border border-slate-200 bg-white p-5 shadow-xs transition-colors hover:border-slate-300"
              >
                <div className="flex items-start justify-between gap-4">
                  <div className="min-w-0 flex-1">
                    <p data-testid="vacancy-title" className="text-base font-semibold text-slate-900">
                      <a href={vacancyHash(vacancy.id)} className="hover:underline">
                        {vacancy.title}
                      </a>
                    </p>
                    <div className="mt-0.5 flex flex-wrap items-center gap-x-2 gap-y-0.5 text-sm text-slate-500">
                      <span data-testid="vacancy-company">{vacancy.company}</span>
                      <span aria-hidden="true">·</span>
                      <time data-testid="vacancy-created" dateTime={vacancy.createdAt} className="text-xs">
                        {formatVacancyDate(vacancy.createdAt)}
                      </time>
                    </div>
                  </div>

                  {vacancy.score !== null && (
                    <div
                      className={`flex shrink-0 flex-col items-center rounded-lg px-3 py-1.5 ${
                        vacancy.failsMustHave
                          ? 'bg-red-50 text-red-700 ring-1 ring-red-200/80'
                          : 'bg-emerald-50 text-emerald-700 ring-1 ring-emerald-200/80'
                      }`}
                    >
                      <span data-testid="vacancy-row-score" className="text-xl font-bold leading-none">
                        {vacancy.score}
                      </span>
                      <span className="mt-0.5 text-[10px] font-medium uppercase tracking-wide opacity-75">
                        / 100
                      </span>
                    </div>
                  )}
                </div>

                {vacancy.failsMustHave && (
                  <p
                    data-testid="vacancy-must-have-failed"
                    className="mt-3 inline-flex items-center rounded-md bg-red-50 px-2.5 py-1 text-xs font-medium text-red-700"
                  >
                    Fails must-have: {vacancy.unmetMustHaves.join(', ')}
                  </p>
                )}

                {vacancy.verdicts.length > 0 && (
                  <ul aria-label="Why this score" className="mt-3 flex flex-col gap-1 border-t border-slate-100 pt-3 text-sm text-slate-600">
                    {vacancy.verdicts.map((verdict) => (
                      <li key={`${verdict.kind}:${verdict.requirement}`} data-testid="vacancy-row-explanation">
                        {verdict.explanation}
                      </li>
                    ))}
                  </ul>
                )}

                <div className="mt-4 flex items-center justify-between border-t border-slate-100 pt-3">
                  <a
                    href={vacancyHash(vacancy.id)}
                    className="text-xs font-medium text-slate-700 hover:text-slate-900 hover:underline"
                  >
                    View details →
                  </a>
                  <div className="flex items-center gap-2">
                    {confirmingId === vacancy.id ? (
                      <span className="flex items-center gap-2 text-xs text-slate-600">
                        <span>Delete this vacancy?</span>
                        <button
                          type="button"
                          disabled={deleting}
                          onClick={() => void confirmDelete(vacancy.id)}
                          aria-label={`Confirm delete ${vacancy.title}`}
                          className="rounded-md bg-red-600 px-2.5 py-1 font-medium text-white hover:bg-red-700 disabled:opacity-50"
                        >
                          Confirm
                        </button>
                        <button
                          type="button"
                          disabled={deleting}
                          onClick={() => setConfirmingId(null)}
                          aria-label={`Cancel delete ${vacancy.title}`}
                          className="rounded-md border border-slate-300 px-2.5 py-1 font-medium text-slate-700 hover:bg-slate-100 disabled:opacity-50"
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
                        className="rounded-md px-2.5 py-1 text-xs font-medium text-slate-500 transition-colors hover:bg-slate-100 hover:text-red-600"
                      >
                        Delete
                      </button>
                    )}
                  </div>
                </div>
              </li>
            ))}
          </ul>

          {lastPage > 1 && (
            <nav aria-label="Vacancy pages" className="flex items-center justify-between border-t border-slate-200 pt-4 text-sm">
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
    </section>
  );
}
