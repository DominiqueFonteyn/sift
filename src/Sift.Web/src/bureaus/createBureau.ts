import { apiClient } from '../api/client';
import type { components } from '../api/generated/schema';

export type CreateBureauRequest = components['schemas']['CreateBureauRequest'];

export type BureauField = 'name' | 'website';

export type BureauFieldErrors = Partial<Record<BureauField, string>>;

export type CreateBureauOutcome = { ok: true } | { ok: false; fieldErrors: BureauFieldErrors; message: string | null };

const fields: BureauField[] = ['name', 'website'];

function serverFieldErrors(error: Partial<components['schemas']['ValidationProblemDetails']> | undefined): BureauFieldErrors {
  const errors = error?.errors ?? {};
  const result: BureauFieldErrors = {};

  for (const field of fields) {
    const message = errors[field]?.[0];
    if (message) {
      result[field] = message;
    }
  }

  return result;
}

// Validation lives on the server only, so the form can never disagree with the API.
export async function createBureau(draft: CreateBureauRequest): Promise<CreateBureauOutcome> {
  try {
    const { data, error } = await apiClient.POST('/bureaus', { body: draft });

    if (data) {
      return { ok: true };
    }

    const fieldErrors = serverFieldErrors(error);
    const message = Object.keys(fieldErrors).length > 0 ? null : (error?.detail ?? error?.title ?? 'Could not save the bureau.');

    return { ok: false, fieldErrors, message };
  } catch (reason: unknown) {
    return {
      ok: false,
      fieldErrors: {},
      message: reason instanceof Error ? reason.message : 'The API could not be reached.',
    };
  }
}
