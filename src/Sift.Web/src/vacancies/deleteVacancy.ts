import { apiClient } from '../api/client';

export type DeleteOutcome = { ok: true } | { ok: false; message: string };

export async function deleteVacancy(id: string): Promise<DeleteOutcome> {
  try {
    const { response, error } = await apiClient.DELETE('/vacancies/{id}', { params: { path: { id } } });

    if (!response.ok) {
      return { ok: false, message: error?.detail ?? error?.title ?? 'Could not delete the vacancy.' };
    }

    return { ok: true };
  } catch (reason: unknown) {
    return { ok: false, message: reason instanceof Error ? reason.message : 'The API could not be reached.' };
  }
}
