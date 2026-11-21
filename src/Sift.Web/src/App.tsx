import { BureausSection } from './bureaus/BureausSection';
import { useHealth } from './health/useHealth';
import { LoginForm } from './identity/LoginForm';
import { RegisterForm } from './identity/RegisterForm';
import { SignedIn } from './identity/SignedIn';
import { useSession } from './identity/useSession';
import { RecruitersSection } from './recruiters/RecruitersSection';
import { RequirementsEditor } from './requirements/RequirementsEditor';
import { ResumeEditor } from './resume/ResumeEditor';
import { NewVacancyPage } from './vacancies/NewVacancyPage';
import { useVacancyView } from './vacancies/useVacancyView';
import { VacancyDetailPage } from './vacancies/VacancyDetailPage';
import { VacancyList } from './vacancies/VacancyList';

function Vacancies() {
  const { view, show } = useVacancyView();

  if (view.kind === 'new') {
    return <NewVacancyPage onSaved={() => show({ kind: 'list' })} onCancel={() => show({ kind: 'list' })} />;
  }

  if (view.kind === 'detail') {
    return <VacancyDetailPage id={view.id} onBack={() => show({ kind: 'list' })} />;
  }

  return <VacancyList onPaste={() => show({ kind: 'new' })} />;
}

export function App() {
  const health = useHealth();
  const { session, expired } = useSession();

  return (
    <main className="flex min-h-screen flex-col items-center justify-center gap-4 bg-slate-50 p-8 text-slate-900">
      <h1 className="text-3xl font-semibold tracking-tight">Sift</h1>
      <p className="text-sm text-slate-500">Walking skeleton</p>
      <section
        aria-label="API health"
        className="rounded-lg border border-slate-200 bg-white px-6 py-4 shadow-sm"
      >
        {health.kind === 'loading' && <p data-testid="health-status">Checking the API…</p>}
        {health.kind === 'loaded' && (
          <p data-testid="health-status">
            API status: <span className="font-semibold">{health.status}</span>
          </p>
        )}
        {health.kind === 'failed' && (
          <p data-testid="health-status" className="text-red-600">
            API unreachable: {health.detail}
          </p>
        )}
      </section>
      {session ? (
        <>
          <SignedIn />
          <Vacancies />
          <BureausSection />
          <RecruitersSection />
          <ResumeEditor />
          <RequirementsEditor />
        </>
      ) : (
        <>
          <LoginForm sessionExpired={expired} />
          <RegisterForm />
        </>
      )}
    </main>
  );
}
