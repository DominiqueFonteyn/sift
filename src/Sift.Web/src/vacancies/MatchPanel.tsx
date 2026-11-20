import type { components } from '../api/generated/schema';
import { MissingInputLinks, missingInputs } from './MissingInputs';

type VacancyMatch = components['schemas']['VacancyMatch'];

const passThreshold = 50;

type MatchPanelProps = {
  match: VacancyMatch;
};

export function MatchPanel({ match }: MatchPanelProps) {
  if (match.score === null || match.missingInputs.length > 0) {
    return (
      <section
        aria-label="Score"
        data-testid="match-missing"
        className="rounded-lg border border-amber-300 bg-amber-50 px-6 py-4 text-sm shadow-sm"
      >
        <p className="font-medium">
          This vacancy cannot be scored yet. Missing: {match.missingInputs.map((input) => missingInputs[input].label).join(', ')}.
        </p>
        <MissingInputLinks inputs={match.missingInputs} testIdPrefix="match-missing" />
      </section>
    );
  }

  const passes = match.score >= passThreshold;

  return (
    <section aria-label="Score" className="rounded-lg border border-slate-200 bg-white px-6 py-4 shadow-sm">
      <p className="text-sm text-slate-600">
        Score{' '}
        <span
          data-testid="vacancy-score"
          data-passes={passes}
          className={`text-2xl font-semibold ${passes ? 'text-green-700' : 'text-red-700'}`}
        >
          {match.score}
        </span>{' '}
        <span className="text-slate-500">/ 100</span>
      </p>
      <ul aria-label="Requirement verdicts" className="mt-3 flex flex-col divide-y divide-slate-100">
        {match.verdicts.map((verdict) => (
          <li
            key={`${verdict.kind}:${verdict.requirement}`}
            data-testid="verdict-item"
            data-requirement={verdict.requirement}
            data-kind={verdict.kind}
            data-verdict={verdict.verdict}
            className="flex flex-col gap-1 py-2 text-sm"
          >
            <div className="flex items-center gap-2">
              <span className="font-medium">{verdict.requirement}</span>
              <span
                data-testid="verdict-kind"
                className={`rounded px-2 py-0.5 text-xs font-medium ${
                  verdict.kind === 'must-have' ? 'bg-slate-900 text-white' : 'bg-slate-100 text-slate-700'
                }`}
              >
                {verdict.kind === 'must-have' ? 'Must-have' : 'Nice-to-have'}
              </span>
              <span
                data-testid="verdict-outcome"
                className={`rounded px-2 py-0.5 text-xs font-medium ${
                  verdict.verdict === 'met' ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'
                }`}
              >
                {verdict.verdict}
              </span>
            </div>
            <p data-testid="verdict-explanation" className="text-slate-600">
              {verdict.explanation}
            </p>
          </li>
        ))}
      </ul>
    </section>
  );
}
