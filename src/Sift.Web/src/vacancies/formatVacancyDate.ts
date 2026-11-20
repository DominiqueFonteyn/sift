// Shared by the list and the detail page so the two can never show different dates for one vacancy.
export function formatVacancyDate(createdAt: string): string {
  return new Date(createdAt).toLocaleDateString();
}
