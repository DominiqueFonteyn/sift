import { useCallback, useEffect, useState } from 'react';

export type VacancyView = 'list' | 'new';

const newHash = '#/vacancies/new';

function readView(): VacancyView {
  return window.location.hash === newHash ? 'new' : 'list';
}

export function useVacancyView() {
  const [view, setView] = useState<VacancyView>(readView);

  useEffect(() => {
    const onHashChange = () => setView(readView());
    window.addEventListener('hashchange', onHashChange);

    return () => window.removeEventListener('hashchange', onHashChange);
  }, []);

  const show = useCallback((next: VacancyView) => {
    window.location.hash = next === 'new' ? newHash : '#/vacancies';
  }, []);

  return { view, show };
}
