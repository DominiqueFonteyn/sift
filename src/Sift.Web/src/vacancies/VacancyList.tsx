import { useState } from 'react';
import { formatVacancyDate } from './formatVacancyDate';
import { useVacancies, vacanciesPageSize } from './useVacancies';
import { vacancyHash } from './useVacancyView';

type VacancyListProps = {
  onPaste: () => void;
};

export function VacancyList({ onPaste }: VacancyListProps) {
  const [page, setPage] = useState(1);
  const state = useVacancies(page);
  const lastPage = state.kind === 'loaded' ? Math.max(1, Math.ceil(state.total / vacanciesPageSize)) : 1;

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
          <ul className="mt-2 divide-y divide-slate-200">
            {state.items.map((vacancy) => (
              <li key={vacancy.id} data-testid="vacancy-row" className="flex items-baseline justify-between gap-4 py-2">
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
                <time data-testid="vacancy-created" dateTime={vacancy.createdAt} className="text-xs text-slate-500">
                  {formatVacancyDate(vacancy.createdAt)}
                </time>
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
