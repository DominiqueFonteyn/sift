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
        className="rounded-xl border border-amber-200 bg-amber-50 p-5 text-sm text-amber-950"
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
    <section aria-label="Score" className="rounded-xl border border-slate-200 bg-white p-6 shadow-xs">
      <div className="flex items-center justify-between gap-4 border-b border-slate-100 pb-4">
        <div>
          <h3 className="text-sm font-semibold uppercase tracking-wider text-slate-400">Match score</h3>
          <p className="mt-1 text-xs text-slate-500">
            {passes ? 'All must-have requirements met.' : 'One or more must-have requirements were not found.'}
          </p>
        </div>
        <div
          className={`flex items-baseline gap-1 rounded-xl px-4 py-2 ${
            passes
              ? 'bg-emerald-50 text-emerald-700 ring-1 ring-emerald-200/80'
              : 'bg-red-50 text-red-700 ring-1 ring-red-200/80'
          }`}
        >
          <span
            data-testid="vacancy-score"
            data-passes={passes}
            className="text-3xl font-bold leading-none"
          >
            {match.score}
          </span>
          <span className="text-xs font-medium opacity-75">/ 100</span>
        </div>
      </div>

      <ul aria-label="Requirement verdicts" className="mt-2 flex flex-col divide-y divide-slate-100">
        {match.verdicts.map((verdict) => (
          <li
            key={`${verdict.kind}:${verdict.requirement}`}
            data-testid="verdict-item"
            data-requirement={verdict.requirement}
            data-kind={verdict.kind}
            data-verdict={verdict.verdict}
            className="flex flex-col gap-1 py-3 text-sm"
          >
            <div className="flex flex-wrap items-center gap-2">
              <span className="font-medium text-slate-900">{verdict.requirement}</span>
              <span
                data-testid="verdict-kind"
                className={`rounded-full px-2.5 py-0.5 text-xs font-medium ${
                  verdict.kind === 'must-have' ? 'bg-slate-900 text-white' : 'bg-slate-100 text-slate-700'
                }`}
              >
                {verdict.kind === 'must-have' ? 'Must-have' : 'Nice-to-have'}
              </span>
              <span
                data-testid="verdict-outcome"
                className={`rounded-full px-2.5 py-0.5 text-xs font-medium ${
                  verdict.verdict === 'met' ? 'bg-emerald-100 text-emerald-800' : 'bg-red-100 text-red-800'
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
