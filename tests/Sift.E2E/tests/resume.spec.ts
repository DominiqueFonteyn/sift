import { expect, test, type APIRequestContext, type Page } from '@playwright/test';

const apiUrl = 'http://localhost:5100';
const password = 'correct horse battery';

function uniqueEmail(): string {
  return `e2e-resume-${Date.now()}-${Math.floor(Math.random() * 1_000_000)}@example.com`;
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
  await page.getByRole('navigation', { name: 'Main navigation' }).getByRole('link', { name: 'Resume' }).click();
}

function editor(page: Page) {
  return page.getByRole('region', { name: 'Resume' });
}

async function saveResume(page: Page, text: string): Promise<void> {
  const resume = editor(page);
  await resume.getByLabel('Resume text').fill(text);
  await resume.getByRole('button', { name: 'Save resume' }).click();
}

test('a user saves a resume, reloads and sees the saved text', async ({ page, request }) => {
  const email = uniqueEmail();
  await registerViaApi(request, email);
  await logIn(page, email);

  await expect(editor(page).getByLabel('Resume text')).toHaveValue('');

  await saveResume(page, 'Senior engineer, 10 years of C# and React.');
  await expect(page.getByTestId('resume-saved')).toBeVisible();

  await page.reload();

  await expect(editor(page).getByLabel('Resume text')).toHaveValue('Senior engineer, 10 years of C# and React.');
});

test('a blank resume shows an inline error and keeps the previous text', async ({ page, request }) => {
  const email = uniqueEmail();
  await registerViaApi(request, email);
  await logIn(page, email);
  await saveResume(page, 'Keep me');
  await expect(page.getByTestId('resume-saved')).toBeVisible();

  await saveResume(page, '   \n  ');

  await expect(page.getByTestId('resume-error')).toHaveText('The resume cannot be empty.');
  await page.reload();
  await expect(editor(page).getByLabel('Resume text')).toHaveValue('Keep me');
});

test('a resume of 20,001 characters shows the limit inline and keeps the previous text', async ({ page, request }) => {
  const email = uniqueEmail();
  await registerViaApi(request, email);
  await logIn(page, email);
  await saveResume(page, 'Keep me');
  await expect(page.getByTestId('resume-saved')).toBeVisible();

  await saveResume(page, 'a'.repeat(20_001));

  await expect(page.getByTestId('resume-error')).toContainText('20,000');
  await page.reload();
  await expect(editor(page).getByLabel('Resume text')).toHaveValue('Keep me');
});

test('a resume of exactly 20,000 characters is saved', async ({ page, request }) => {
  const email = uniqueEmail();
  await registerViaApi(request, email);
  await logIn(page, email);
  const text = 'b'.repeat(20_000);

  await saveResume(page, text);
  await expect(page.getByTestId('resume-saved')).toBeVisible();

  await page.reload();
  await expect(editor(page).getByLabel('Resume text')).toHaveValue(text);
});
