import { useCallback } from 'react';
import { apiClient } from '../api/client';
import { validateVacancy, type CreateVacancyRequest, type VacancyField, type VacancyFieldErrors } from './vacancyRules';

export type CreateOutcome = { ok: true } | { ok: false; fieldErrors: VacancyFieldErrors; message: string | null };

const fields: VacancyField[] = ['title', 'company', 'rawText'];

function serverFieldErrors(error: unknown): VacancyFieldErrors {
  const errors = (error as { errors?: Record<string, string[]> } | undefined)?.errors ?? {};
  const result: VacancyFieldErrors = {};

  for (const field of fields) {
    const message = errors[field]?.[0];
    if (message) {
      result[field] = message;
    }
  }

  return result;
}

export function useCreateVacancy() {
  return useCallback(async (draft: CreateVacancyRequest): Promise<CreateOutcome> => {
    const invalid = validateVacancy(draft);
    if (Object.keys(invalid).length > 0) {
      return { ok: false, fieldErrors: invalid, message: null };
    }

    try {
      const { data, error } = await apiClient.POST('/vacancies', { body: draft });

      if (data) {
        return { ok: true };
      }

      const fieldErrors = serverFieldErrors(error);
      const message = Object.keys(fieldErrors).length > 0 ? null : (error?.detail ?? error?.title ?? 'Could not save the vacancy.');

      return { ok: false, fieldErrors, message };
    } catch (reason: unknown) {
      return {
        ok: false,
        fieldErrors: {},
        message: reason instanceof Error ? reason.message : 'The API could not be reached.',
      };
    }
  }, []);
}
