import { expect, test, type APIRequestContext, type Page } from '@playwright/test';

const apiUrl = 'http://localhost:5100';
const password = 'correct horse battery';

function uniqueEmail(): string {
  return `e2e-vacancy-delete-${Date.now()}-${Math.floor(Math.random() * 1_000_000)}@example.com`;
}

async function registerViaApi(request: APIRequestContext, email: string): Promise<void> {
  const response = await request.post(`${apiUrl}/auth/register`, { data: { email, password } });
  expect(response.status()).toBe(201);
}

async function accessToken(request: APIRequestContext, email: string): Promise<string> {
  const login = await request.post(`${apiUrl}/auth/login`, { data: { email, password } });
  expect(login.ok()).toBe(true);
  return ((await login.json()) as { accessToken: string }).accessToken;
}

async function createVacancyViaApi(request: APIRequestContext, email: string, title: string): Promise<string> {
  const response = await request.post(`${apiUrl}/vacancies`, {
    headers: { Authorization: `Bearer ${await accessToken(request, email)}` },
    data: { title, company: 'Acme', rawText: 'Private text' },
  });
  expect(response.status()).toBe(201);
  return ((await response.json()) as { id: string }).id;
}

async function logIn(page: Page, email: string): Promise<void> {
  await page.goto('/');
  const form = page.getByRole('region', { name: 'Login' });
  await form.getByLabel('Email').fill(email);
  await form.getByLabel('Password').fill(password);
  await form.getByRole('button', { name: 'Log in' }).click();
  await expect(page.getByTestId('signed-in')).toContainText(email);
}

function list(page: Page) {
  return page.getByRole('region', { name: 'Vacancies' });
}

test('deleting a vacancy after confirming removes it from the list', async ({ page, request }) => {
  const email = uniqueEmail();
  await registerViaApi(request, email);
  await createVacancyViaApi(request, email, 'Keep Me');
  await createVacancyViaApi(request, email, 'Backend Engineer');
  await logIn(page, email);
  await expect(list(page).getByTestId('vacancy-row')).toHaveCount(2);

  await list(page).getByRole('button', { name: 'Delete Backend Engineer' }).click();
  await list(page).getByRole('button', { name: 'Confirm delete Backend Engineer' }).click();

  await expect(list(page).getByTestId('vacancy-row')).toHaveCount(1);
  await expect(list(page).getByText('Backend Engineer')).toHaveCount(0);
  await expect(list(page).getByText('Keep Me')).toBeVisible();
});

test('cancelling the confirmation keeps the vacancy in the list', async ({ page, request }) => {
  const email = uniqueEmail();
  await registerViaApi(request, email);
  await createVacancyViaApi(request, email, 'Backend Engineer');
  await logIn(page, email);

  await list(page).getByRole('button', { name: 'Delete Backend Engineer' }).click();
  await list(page).getByRole('button', { name: 'Cancel delete Backend Engineer' }).click();

  await expect(list(page).getByTestId('vacancy-row')).toHaveCount(1);
  await expect(list(page).getByTestId('vacancy-title')).toHaveText('Backend Engineer');
  await expect(list(page).getByRole('button', { name: 'Delete Backend Engineer' })).toBeVisible();
});

test('confirming delete for a vacancy already deleted elsewhere shows an error and refreshes the list', async ({
  page,
  request,
}) => {
  const email = uniqueEmail();
  await registerViaApi(request, email);
  await createVacancyViaApi(request, email, 'Keep Me');
  const doomed = await createVacancyViaApi(request, email, 'Backend Engineer');
  await logIn(page, email);
  await expect(list(page).getByTestId('vacancy-row')).toHaveCount(2);

  await list(page).getByRole('button', { name: 'Delete Backend Engineer' }).click();
  const other = await request.delete(`${apiUrl}/vacancies/${doomed}`, {
    headers: { Authorization: `Bearer ${await accessToken(request, email)}` },
  });
  expect(other.status()).toBe(204);
  await list(page).getByRole('button', { name: 'Confirm delete Backend Engineer' }).click();

  await expect(list(page).getByTestId('vacancy-delete-error')).toBeVisible();
  await expect(list(page).getByTestId('vacancy-row')).toHaveCount(1);
  await expect(list(page).getByText('Backend Engineer')).toHaveCount(0);
});
