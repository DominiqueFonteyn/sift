import { expect, test, type APIRequestContext, type Page } from '@playwright/test';

const apiUrl = 'http://localhost:5100';
const password = 'correct horse battery';

function uniqueEmail(): string {
  return `e2e-nav-${Date.now()}-${Math.floor(Math.random() * 1_000_000)}@example.com`;
}

async function registerViaApi(request: APIRequestContext, email: string): Promise<void> {
  const response = await request.post(`${apiUrl}/auth/register`, { data: { email, password } });
  expect(response.status()).toBe(201);
}

async function logIn(page: Page, email: string): Promise<void> {
  await page.goto('/');
  const form = page.getByRole('region', { name: 'Login' });
  await form.getByLabel('Email').fill(email);
  await form.getByLabel('Password').fill(password);
  await form.getByRole('button', { name: 'Log in' }).click();
  await expect(page.getByTestId('signed-in')).toContainText(email);
}

test('each workspace area is focused on its own view with vacancies as the default', async ({ page, request }) => {
  const email = uniqueEmail();
  await registerViaApi(request, email);
  await logIn(page, email);

  // Default landing page is Vacancies, without other sections cluttering the screen.
  await expect(page.getByRole('region', { name: 'Vacancies' })).toBeVisible();
  await expect(page.getByRole('region', { name: 'Resume' })).toHaveCount(0);
  await expect(page.getByRole('region', { name: 'Requirements' })).toHaveCount(0);
  await expect(page.getByRole('region', { name: 'Recruiters' })).toHaveCount(0);
  await expect(page.getByRole('region', { name: 'Bureaus', exact: true })).toHaveCount(0);

  const nav = page.getByRole('navigation', { name: 'Main navigation' });

  // Switching to Resume hides Vacancies and shows only Resume, surviving reload.
  await nav.getByRole('link', { name: 'Resume' }).click();
  await expect(page.getByRole('region', { name: 'Resume' })).toBeVisible();
  await expect(page.getByRole('region', { name: 'Vacancies' })).toHaveCount(0);
  await expect(nav.getByRole('link', { name: 'Resume' })).toHaveAttribute('aria-current', 'page');

  await page.reload();
  await expect(page.getByRole('region', { name: 'Resume' })).toBeVisible();
  await expect(page.getByRole('region', { name: 'Vacancies' })).toHaveCount(0);

  // Switching to Requirements, Recruiters, Bureaus, and back to Vacancies keeps each view isolated.
  await nav.getByRole('link', { name: 'Requirements' }).click();
  await expect(page.getByRole('region', { name: 'Requirements' })).toBeVisible();
  await expect(page.getByRole('region', { name: 'Resume' })).toHaveCount(0);

  await nav.getByRole('link', { name: 'Recruiters' }).click();
  await expect(page.getByRole('region', { name: 'Recruiters' })).toBeVisible();
  await expect(page.getByRole('region', { name: 'Requirements' })).toHaveCount(0);

  await nav.getByRole('link', { name: 'Bureaus' }).click();
  await expect(page.getByRole('region', { name: 'Bureaus', exact: true })).toBeVisible();
  await expect(page.getByRole('region', { name: 'Recruiters' })).toHaveCount(0);

  await nav.getByRole('link', { name: 'Vacancies' }).click();
  await expect(page.getByRole('region', { name: 'Vacancies' })).toBeVisible();
  await expect(page.getByRole('region', { name: 'Bureaus', exact: true })).toHaveCount(0);
});
