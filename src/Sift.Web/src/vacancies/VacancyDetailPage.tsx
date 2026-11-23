import { vacanciesHash } from '../navigation/routes';
import { formatVacancyDate } from './formatVacancyDate';
import { MatchPanel } from './MatchPanel';
import { useVacancy } from './useVacancy';

type VacancyDetailPageProps = {
  id: string;
  onBack: () => void;
};

export function VacancyDetailPage({ id, onBack }: VacancyDetailPageProps) {
  const state = useVacancy(id);

  return (
    <section aria-label="Vacancy" className="flex flex-col gap-6">
      <div>
        <a
          href={vacanciesHash}
          data-testid="vacancy-back"
          onClick={(event) => {
            event.preventDefault();
            onBack();
          }}
          className="inline-flex items-center text-sm font-medium text-slate-600 transition-colors hover:text-slate-900"
        >
          ← Back to my vacancies
        </a>
      </div>

      {state.kind === 'loading' && (
        <p data-testid="vacancy-loading" className="text-sm text-slate-500">
          Loading the vacancy…
        </p>
      )}
      {state.kind === 'failed' && (
        <p
          data-testid="vacancy-error"
          className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700"
        >
          {state.detail}
        </p>
      )}
      {state.kind === 'not-found' && (
        <p
          data-testid="vacancy-not-found"
          className="rounded-xl border border-slate-200 bg-white px-6 py-8 text-center text-sm text-slate-600 shadow-xs"
        >
          We could not find that vacancy.
        </p>
      )}

      {state.kind === 'loaded' && (
        <>
          <header>
            <h2 data-testid="vacancy-detail-title" className="text-2xl font-semibold tracking-tight text-slate-900">
              {state.vacancy.title}
            </h2>
            <div className="mt-1 flex flex-wrap items-center gap-x-2 text-sm text-slate-500">
              <span data-testid="vacancy-detail-company" className="font-medium text-slate-700">
                {state.vacancy.company}
              </span>
              <span aria-hidden="true">·</span>
              <time data-testid="vacancy-detail-created" dateTime={state.vacancy.createdAt} className="text-xs">
                {formatVacancyDate(state.vacancy.createdAt)}
              </time>
            </div>
          </header>

          <MatchPanel match={state.vacancy.match} />

          <article className="rounded-xl border border-slate-200 bg-white p-6 shadow-xs">
            <h3 className="text-sm font-semibold uppercase tracking-wider text-slate-400">Vacancy description</h3>
            <p
              data-testid="vacancy-detail-text"
              className="mt-4 whitespace-pre-wrap break-words text-sm leading-relaxed text-slate-700"
            >
              {state.vacancy.rawText}
            </p>
          </article>
        </>
      )}
    </section>
  );
}
