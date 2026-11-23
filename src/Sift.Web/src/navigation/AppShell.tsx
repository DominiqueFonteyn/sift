import type { ReactNode } from 'react';
import { HealthBadge } from '../health/HealthBadge';
import type { HealthState } from '../health/useHealth';
import { SignedIn } from '../identity/SignedIn';
import { sectionHash, vacanciesHash, type NavSection } from './routes';

type NavGroup = {
  heading?: string;
  items: { section: NavSection; label: string }[];
};

const navGroups: NavGroup[] = [
  {
    items: [{ section: 'vacancies', label: 'Vacancies' }],
  },
  {
    heading: 'My profile',
    items: [
      { section: 'requirements', label: 'Requirements' },
      { section: 'resume', label: 'Resume' },
    ],
  },
  {
    heading: 'Network',
    items: [
      { section: 'recruiters', label: 'Recruiters' },
      { section: 'bureaus', label: 'Bureaus' },
    ],
  },
];

type AppShellProps = {
  activeSection: NavSection;
  health: HealthState;
  children: ReactNode;
};

export function AppShell({ activeSection, health, children }: AppShellProps) {
  return (
    <div className="flex min-h-screen flex-col bg-slate-50 text-slate-900 md:flex-row">
      <aside className="flex shrink-0 flex-col border-b border-slate-200 bg-white md:sticky md:top-0 md:h-screen md:w-64 md:border-r md:border-b-0">
        <div className="flex items-center justify-between border-b border-slate-100 px-5 py-4 md:block md:py-5">
          <a href={vacanciesHash} className="block">
            <h1 className="text-xl font-semibold tracking-tight text-slate-900">Sift</h1>
            <p className="text-xs text-slate-500">Vacancy matcher</p>
          </a>
        </div>

        <nav
          aria-label="Main navigation"
          className="flex flex-1 flex-row gap-4 overflow-x-auto px-4 py-3 md:flex-col md:gap-6 md:overflow-visible md:px-3 md:py-5"
        >
          {navGroups.map((group, index) => (
            <div key={group.heading ?? index} className="flex items-center gap-1 md:flex-col md:items-stretch md:gap-1">
              {group.heading && (
                <p className="hidden px-3 pb-1 text-xs font-semibold uppercase tracking-wider text-slate-400 md:block">
                  {group.heading}
                </p>
              )}
              {group.items.map(({ section, label }) => {
                const active = activeSection === section;

                return (
                  <a
                    key={section}
                    href={sectionHash(section)}
                    aria-current={active ? 'page' : undefined}
                    className={
                      active
                        ? 'whitespace-nowrap rounded-lg bg-slate-900 px-3 py-2 text-sm font-medium text-white shadow-xs'
                        : 'whitespace-nowrap rounded-lg px-3 py-2 text-sm font-medium text-slate-600 transition-colors hover:bg-slate-100 hover:text-slate-900'
                    }
                  >
                    {label}
                  </a>
                );
              })}
            </div>
          ))}
        </nav>

        <div className="flex flex-col gap-3 border-t border-slate-100 px-5 py-4">
          <SignedIn />
          <HealthBadge health={health} />
        </div>
      </aside>

      <main className="flex-1 px-4 py-6 sm:px-8 md:px-10 md:py-10">
        <div className="mx-auto w-full max-w-4xl">{children}</div>
      </main>
    </div>
  );
}
