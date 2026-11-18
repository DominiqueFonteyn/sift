// Mirrors the TypeSpec limit (api/resumes.tsp). Both sides count UTF-16 code units: JS .length equals C# string.Length.
export const resumeMaxLength = 20_000;

export const emptyResumeMessage = 'The resume cannot be empty.';

export const tooLongResumeMessage = `The resume can be at most ${resumeMaxLength.toLocaleString('en-US')} characters.`;

export function validateResume(text: string): string | null {
  if (text.trim().length === 0) {
    return emptyResumeMessage;
  }

  return text.length > resumeMaxLength ? tooLongResumeMessage : null;
}
