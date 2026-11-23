import { BureausSection } from './bureaus/BureausSection';
import { useHealth } from './health/useHealth';
import { AuthScreen } from './identity/AuthScreen';
import { useSession } from './identity/useSession';
import { AppShell } from './navigation/AppShell';
import { newVacancyHash, vacanciesHash, type AppRoute } from './navigation/routes';
import { useRoute } from './navigation/useRoute';
import { RecruitersSection } from './recruiters/RecruitersSection';
import { RequirementsEditor } from './requirements/RequirementsEditor';
import { ResumeEditor } from './resume/ResumeEditor';
import { NewVacancyPage } from './vacancies/NewVacancyPage';
import { VacancyDetailPage } from './vacancies/VacancyDetailPage';
import { VacancyList } from './vacancies/VacancyList';

function ActivePage({ route, navigate }: { route: AppRoute; navigate: (hash: string) => void }) {
  switch (route.section) {
    case 'vacancies':
      if (route.view.kind === 'new') {
        return (
          <NewVacancyPage
            onSaved={() => navigate(vacanciesHash)}
            onCancel={() => navigate(vacanciesHash)}
          />
        );
      }

      if (route.view.kind === 'detail') {
        return <VacancyDetailPage id={route.view.id} onBack={() => navigate(vacanciesHash)} />;
      }

      return <VacancyList onPaste={() => navigate(newVacancyHash)} />;

    case 'requirements':
      return <RequirementsEditor />;

    case 'resume':
      return <ResumeEditor />;

    case 'recruiters':
      return <RecruitersSection />;

    case 'bureaus':
      return <BureausSection view={route.view} onNavigate={navigate} />;
  }
}

export function App() {
  const health = useHealth();
  const { session, expired } = useSession();
  const { route, navigate } = useRoute();

  if (!session) {
    return <AuthScreen sessionExpired={expired} health={health} />;
  }

  return (
    <AppShell activeSection={route.section} health={health}>
      <ActivePage route={route} navigate={navigate} />
    </AppShell>
  );
}
