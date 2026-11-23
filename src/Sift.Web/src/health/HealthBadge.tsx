import type { HealthState } from './useHealth';

type HealthBadgeProps = {
  health: HealthState;
};

export function HealthBadge({ health }: HealthBadgeProps) {
  const dotClass =
    health.kind === 'loaded'
      ? 'bg-emerald-500'
      : health.kind === 'failed'
        ? 'bg-red-500'
        : 'bg-slate-400';

  return (
    <section aria-label="API health" className="flex items-center gap-2 text-xs text-slate-500">
      <span aria-hidden="true" className={`h-2 w-2 shrink-0 rounded-full ${dotClass}`} />
      {health.kind === 'loading' && <p data-testid="health-status">Checking the API…</p>}
      {health.kind === 'loaded' && (
        <p data-testid="health-status">
          API status: <span className="font-medium text-slate-700">{health.status}</span>
        </p>
      )}
      {health.kind === 'failed' && (
        <p data-testid="health-status" className="text-red-600">
          API unreachable: {health.detail}
        </p>
      )}
    </section>
  );
}
