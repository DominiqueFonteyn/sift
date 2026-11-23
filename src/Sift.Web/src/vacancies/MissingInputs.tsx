import type { components } from '../api/generated/schema';
import { requirementsHash, resumeHash } from '../navigation/routes';

type MatchInput = components['schemas']['MatchInput'];

export const missingInputs: Record<MatchInput, { label: string; hash: string; linkText: string }> = {
  resume: { label: 'resume', hash: resumeHash, linkText: 'my resume' },
  requirements: { label: 'requirements', hash: requirementsHash, linkText: 'my requirements' },
};

type MissingInputLinksProps = {
  inputs: MatchInput[];
  testIdPrefix: string;
};

export function MissingInputLinks({ inputs, testIdPrefix }: MissingInputLinksProps) {
  return (
    <ul className="mt-2 flex flex-wrap gap-3">
      {inputs.map((input) => (
        <li key={input}>
          <a
            href={missingInputs[input].hash}
            data-testid={`${testIdPrefix}-${input}`}
            className="inline-flex items-center rounded-md bg-amber-900/10 px-2.5 py-1 text-xs font-medium text-amber-950 transition-colors hover:bg-amber-900/20"
          >
            Add {missingInputs[input].linkText} →
          </a>
        </li>
      ))}
    </ul>
  );
}
