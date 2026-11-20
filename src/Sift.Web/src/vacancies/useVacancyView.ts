import { useCallback, useEffect, useState } from 'react';

export type VacancyView = { kind: 'list' } | { kind: 'new' } | { kind: 'detail'; id: string };

const listHash = '#/vacancies';
const newHash = '#/vacancies/new';
const detailPrefix = '#/vacancies/';

function safeDecode(value: string): string {
  try {
    return decodeURIComponent(value);
  } catch {
    return value;
  }
}

function readView(): VacancyView {
  const hash = window.location.hash;

  if (hash === newHash) {
    return { kind: 'new' };
  }

  if (hash.startsWith(detailPrefix) && hash.length > detailPrefix.length) {
    return { kind: 'detail', id: safeDecode(hash.slice(detailPrefix.length)) };
  }

  return { kind: 'list' };
}

export function vacancyHash(id: string): string {
  return `${detailPrefix}${encodeURIComponent(id)}`;
}

export function useVacancyView() {
  const [view, setView] = useState<VacancyView>(readView);

  useEffect(() => {
    const onHashChange = () => setView(readView());
    window.addEventListener('hashchange', onHashChange);

    return () => window.removeEventListener('hashchange', onHashChange);
  }, []);

  const show = useCallback((next: VacancyView) => {
    window.location.hash =
      next.kind === 'new' ? newHash : next.kind === 'detail' ? vacancyHash(next.id) : listHash;
  }, []);

  return { view, show };
}
