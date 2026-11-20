import { expect, test, type APIRequestContext, type Page } from '@playwright/test';

const apiUrl = 'http://localhost:5100';
const password = 'correct horse battery';

function uniqueEmail(): string {
  return `e2e-requirements-${Date.now()}-${Math.floor(Math.random() * 1_000_000)}@example.com`;
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

function requirements(page: Page) {
  return page.getByRole('region', { name: 'Requirements' });
}

async function addRequirement(page: Page, text: string, kind: 'must-have' | 'nice-to-have'): Promise<void> {
  const section = requirements(page);
  await section.getByLabel('Requirement', { exact: true }).fill(text);
  await section.getByLabel('Kind').selectOption(kind);
  await section.getByRole('button', { name: 'Add requirement' }).click();
}

function item(page: Page, text: string) {
  return requirements(page).getByTestId('requirement-item').filter({ hasText: text });
}

test('a user adds a must-have and a nice-to-have, deletes one, and the deletion survives a reload', async ({
  page,
  request,
}) => {
  const email = uniqueEmail();
  await registerViaApi(request, email);
  await logIn(page, email);

  await addRequirement(page, 'Remote-first', 'must-have');
  await expect(item(page, 'Remote-first')).toBeVisible();
  await expect(item(page, 'Remote-first').getByTestId('requirement-kind')).toHaveText('Must-have');

  await addRequirement(page, 'Kotlin', 'nice-to-have');
  await expect(item(page, 'Kotlin')).toBeVisible();
  await expect(item(page, 'Kotlin').getByTestId('requirement-kind')).toHaveText('Nice-to-have');

  await item(page, 'Kotlin').getByRole('button', { name: 'Delete Kotlin' }).click();
  await expect(item(page, 'Kotlin')).toHaveCount(0);
  await expect(item(page, 'Remote-first')).toBeVisible();

  await page.reload();

  await expect(item(page, 'Remote-first')).toBeVisible();
  await expect(item(page, 'Kotlin')).toHaveCount(0);
});

test('a duplicate requirement is rejected with an inline error', async ({ page, request }) => {
  const email = uniqueEmail();
  await registerViaApi(request, email);
  await logIn(page, email);
  await addRequirement(page, 'Remote-first', 'must-have');
  await expect(item(page, 'Remote-first')).toBeVisible();

  await addRequirement(page, 'remote-first', 'must-have');

  await expect(page.getByTestId('requirement-error')).toBeVisible();
  await expect(requirements(page).getByTestId('requirement-item')).toHaveCount(1);
});
