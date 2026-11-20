// Mirror the TypeSpec limits (api/vacancies.tsp). Both sides count UTF-16 code units: JS .length equals C# string.Length.
export const titleMaxLength = 200;
export const companyMaxLength = 200;
export const rawTextMaxLength = 20_000;

export type VacancyField = 'title' | 'company' | 'rawText';

export type VacancyFieldErrors = Partial<Record<VacancyField, string>>;

export type VacancyDraft = { title: string; company: string; rawText: string };

function validateField(value: string, label: string, max: number): string | null {
  if (value.trim().length === 0) {
    return `${label} is required.`;
  }

  return value.length > max ? `${label} can be at most ${max.toLocaleString('en-US')} characters.` : null;
}

export function validateVacancy(draft: VacancyDraft): VacancyFieldErrors {
  const errors: VacancyFieldErrors = {};
  const title = validateField(draft.title, 'Title', titleMaxLength);
  const company = validateField(draft.company, 'Company', companyMaxLength);
  const rawText = validateField(draft.rawText, 'Vacancy text', rawTextMaxLength);

  if (title) errors.title = title;
  if (company) errors.company = company;
  if (rawText) errors.rawText = rawText;

  return errors;
}
