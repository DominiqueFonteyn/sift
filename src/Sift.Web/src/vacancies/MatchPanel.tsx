import type { MouseEvent } from 'react';
import type { components } from '../api/generated/schema';

type VacancyMatch = components['schemas']['VacancyMatch'];
type MatchInput = components['schemas']['MatchInput'];

const passThreshold = 50;

const missingInputs: Record<MatchInput, { label: string; target: string; linkText: string }> = {
  resume: { label: 'resume', target: 'resume', linkText: 'my resume' },
  requirements: { label: 'requirements', target: 'requirements', linkText: 'my requirements' },
};

// The resume and requirements editors sit on the same page, so the links scroll to them
// instead of changing the route, which would leave the vacancy.
function goTo(target: string) {
  return (event: MouseEvent<HTMLAnchorElement>) => {
    const section = document.getElementById(target);
    if (section) {
      event.preventDefault();
      section.scrollIntoView({ block: 'start' });
      section.focus({ preventScroll: true });
    }
  };
}

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
        <ul className="mt-2 flex flex-col gap-1">
          {match.missingInputs.map((input) => (
            <li key={input}>
              <a
                href={`#${missingInputs[input].target}`}
                data-testid={`match-missing-${input}`}
                onClick={goTo(missingInputs[input].target)}
                className="font-medium underline"
              >
                Add {missingInputs[input].linkText}
              </a>
            </li>
          ))}
        </ul>
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
