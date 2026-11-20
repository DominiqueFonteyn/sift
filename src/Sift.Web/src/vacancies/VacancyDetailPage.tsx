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
    <section aria-label="Vacancy" className="flex w-full max-w-2xl flex-col gap-4">
      <a
        href="#/vacancies"
        data-testid="vacancy-back"
        onClick={(event) => {
          event.preventDefault();
          onBack();
        }}
        className="text-sm font-medium underline"
      >
        Back to my vacancies
      </a>
      {state.kind === 'loading' && <p data-testid="vacancy-loading">Loading the vacancy…</p>}
      {state.kind === 'failed' && (
        <p data-testid="vacancy-error" className="text-red-600">
          {state.detail}
        </p>
      )}
      {state.kind === 'not-found' && (
        <p data-testid="vacancy-not-found" className="rounded-lg border border-slate-200 bg-white px-6 py-4 shadow-sm">
          We could not find that vacancy.
        </p>
      )}
      {state.kind === 'loaded' && <MatchPanel match={state.vacancy.match} />}
      {state.kind === 'loaded' && (
        <article className="rounded-lg border border-slate-200 bg-white px-6 py-4 shadow-sm">
          <h2 data-testid="vacancy-detail-title" className="text-lg font-semibold">
            {state.vacancy.title}
          </h2>
          <p data-testid="vacancy-detail-company" className="text-sm text-slate-600">
            {state.vacancy.company}
          </p>
          <time
            data-testid="vacancy-detail-created"
            dateTime={state.vacancy.createdAt}
            className="mt-1 block text-xs text-slate-500"
          >
            {formatVacancyDate(state.vacancy.createdAt)}
          </time>
          <p data-testid="vacancy-detail-text" className="mt-4 whitespace-pre-wrap break-words text-sm">
            {state.vacancy.rawText}
          </p>
        </article>
      )}
    </section>
  );
}
