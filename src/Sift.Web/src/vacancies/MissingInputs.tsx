import type { MouseEvent } from 'react';
import type { components } from '../api/generated/schema';

type MatchInput = components['schemas']['MatchInput'];

export const missingInputs: Record<MatchInput, { label: string; target: string; linkText: string }> = {
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

type MissingInputLinksProps = {
  inputs: MatchInput[];
  testIdPrefix: string;
};

export function MissingInputLinks({ inputs, testIdPrefix }: MissingInputLinksProps) {
  return (
    <ul className="mt-2 flex flex-col gap-1">
      {inputs.map((input) => (
        <li key={input}>
          <a
            href={`#${missingInputs[input].target}`}
            data-testid={`${testIdPrefix}-${input}`}
            onClick={goTo(missingInputs[input].target)}
            className="font-medium underline"
          >
            Add {missingInputs[input].linkText}
          </a>
        </li>
      ))}
    </ul>
  );
}
