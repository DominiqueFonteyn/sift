import { apiClient } from '../api/client';
import type { components } from '../api/generated/schema';

export type CreateRecruiterRequest = components['schemas']['CreateRecruiterRequest'];

export type RecruiterField = 'name' | 'email' | 'phone' | 'linkedInUrl';

export type RecruiterFieldErrors = Partial<Record<RecruiterField, string>>;

export type CreateRecruiterOutcome = { ok: true } | { ok: false; fieldErrors: RecruiterFieldErrors; message: string | null };

const fields: RecruiterField[] = ['name', 'email', 'phone', 'linkedInUrl'];

function serverFieldErrors(error: Partial<components['schemas']['ValidationProblemDetails']> | undefined): RecruiterFieldErrors {
  const errors = error?.errors ?? {};
  const result: RecruiterFieldErrors = {};

  for (const field of fields) {
    const message = errors[field]?.[0];
    if (message) {
      result[field] = message;
    }
  }

  return result;
}

// Validation lives on the server only, so the form can never disagree with the API.
export async function createRecruiter(draft: CreateRecruiterRequest): Promise<CreateRecruiterOutcome> {
  try {
    const { data, error } = await apiClient.POST('/recruiters', { body: draft });

    if (data) {
      return { ok: true };
    }

    const fieldErrors = serverFieldErrors(error);
    const message = Object.keys(fieldErrors).length > 0 ? null : (error?.detail ?? error?.title ?? 'Could not save the recruiter.');

    return { ok: false, fieldErrors, message };
  } catch (reason: unknown) {
    return {
      ok: false,
      fieldErrors: {},
      message: reason instanceof Error ? reason.message : 'The API could not be reached.',
    };
  }
}
