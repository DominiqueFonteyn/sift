export type VacancyView = { kind: 'list' } | { kind: 'new' } | { kind: 'detail'; id: string };
export type BureauView = { kind: 'list' } | { kind: 'detail'; id: string };

export type AppRoute =
  | { section: 'vacancies'; view: VacancyView }
  | { section: 'requirements' }
  | { section: 'resume' }
  | { section: 'recruiters' }
  | { section: 'bureaus'; view: BureauView };

export type NavSection = AppRoute['section'];

export const vacanciesHash = '#/vacancies';
export const newVacancyHash = '#/vacancies/new';
export const requirementsHash = '#/requirements';
export const resumeHash = '#/resume';
export const recruitersHash = '#/recruiters';
export const bureausHash = '#/bureaus';

const vacancyDetailPrefix = '#/vacancies/';
const bureauDetailPrefix = '#/bureaus/';

function safeDecode(value: string): string {
  try {
    return decodeURIComponent(value);
  } catch {
    return value;
  }
}

export function vacancyHash(id: string): string {
  return `${vacancyDetailPrefix}${encodeURIComponent(id)}`;
}

export function bureauHash(id: string): string {
  return `${bureauDetailPrefix}${encodeURIComponent(id)}`;
}

export function sectionHash(section: NavSection): string {
  switch (section) {
    case 'vacancies':
      return vacanciesHash;
    case 'requirements':
      return requirementsHash;
    case 'resume':
      return resumeHash;
    case 'recruiters':
      return recruitersHash;
    case 'bureaus':
      return bureausHash;
  }
}

export function parseRoute(hash: string): AppRoute {
  if (hash === newVacancyHash) {
    return { section: 'vacancies', view: { kind: 'new' } };
  }

  if (hash.startsWith(vacancyDetailPrefix) && hash.length > vacancyDetailPrefix.length) {
    return { section: 'vacancies', view: { kind: 'detail', id: safeDecode(hash.slice(vacancyDetailPrefix.length)) } };
  }

  if (hash === requirementsHash) {
    return { section: 'requirements' };
  }

  if (hash === resumeHash) {
    return { section: 'resume' };
  }

  if (hash === recruitersHash) {
    return { section: 'recruiters' };
  }

  if (hash === bureausHash) {
    return { section: 'bureaus', view: { kind: 'list' } };
  }

  if (hash.startsWith(bureauDetailPrefix) && hash.length > bureauDetailPrefix.length) {
    return { section: 'bureaus', view: { kind: 'detail', id: safeDecode(hash.slice(bureauDetailPrefix.length)) } };
  }

  return { section: 'vacancies', view: { kind: 'list' } };
}
