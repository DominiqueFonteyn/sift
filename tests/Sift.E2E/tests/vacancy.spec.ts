import { expect, test, type APIRequestContext, type Page } from '@playwright/test';

const apiUrl = 'http://localhost:5100';
const password = 'correct horse battery';
// A fixed zone far from UTC so the date shown is the browser-local one, not the UTC one.
const timezoneId = 'Pacific/Auckland';

test.use({ timezoneId, locale: 'en-US' });

function uniqueEmail(): string {
  return `e2e-vacancy-${Date.now()}-${Math.floor(Math.random() * 1_000_000)}@example.com`;
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

async function createVacancyViaApi(request: APIRequestContext, email: string, title: string): Promise<void> {
  const login = await request.post(`${apiUrl}/auth/login`, { data: { email, password } });
  expect(login.ok()).toBe(true);
  const { accessToken } = (await login.json()) as { accessToken: string };

  const response = await request.post(`${apiUrl}/vacancies`, {
    headers: { Authorization: `Bearer ${accessToken}` },
    data: { title, company: 'Acme', rawText: 'Private text' },
  });
  expect(response.status()).toBe(201);
}

function list(page: Page) {
  return page.getByRole('region', { name: 'Vacancies' });
}

function form(page: Page) {
  return page.getByRole('region', { name: 'New vacancy' });
}

test('a user pastes a vacancy and sees it at the top of the list', async ({ page, request }) => {
  const email = uniqueEmail();
  await registerViaApi(request, email);
  await logIn(page, email);

  await list(page).getByRole('link', { name: 'Paste a vacancy' }).click();
  await form(page).getByLabel('Title').fill('Older Role');
  await form(page).getByLabel('Company').fill('Initech');
  await form(page).getByLabel('Vacancy text').fill('First text');
  await form(page).getByRole('button', { name: 'Save vacancy' }).click();
  await expect(list(page).getByTestId('vacancy-row')).toHaveCount(1);

  await list(page).getByRole('link', { name: 'Paste a vacancy' }).click();
  await form(page).getByLabel('Title').fill('Backend Engineer');
  await form(page).getByLabel('Company').fill('Acme');
  await form(page).getByLabel('Vacancy text').fill('We need C# and SQL');
  const created = page.waitForResponse((response) => response.url().endsWith('/vacancies') && response.request().method() === 'POST');
  await form(page).getByRole('button', { name: 'Save vacancy' }).click();
  const createdAt = ((await (await created).json()) as { createdAt: string }).createdAt;

  await expect(list(page)).toBeVisible();
  const rows = list(page).getByTestId('vacancy-row');
  await expect(rows).toHaveCount(2);
  const first = rows.first();
  await expect(first.getByTestId('vacancy-title')).toHaveText('Backend Engineer');
  await expect(first.getByTestId('vacancy-company')).toHaveText('Acme');
  await expect(first.getByTestId('vacancy-created')).toHaveText(
    new Date(createdAt).toLocaleDateString('en-US', { timeZone: timezoneId }),
  );
  await expect(rows.nth(1).getByTestId('vacancy-title')).toHaveText('Older Role');
});

test('an empty title shows an inline error next to the title and saves nothing', async ({ page, request }) => {
  const email = uniqueEmail();
  await registerViaApi(request, email);
  await logIn(page, email);

  await list(page).getByRole('link', { name: 'Paste a vacancy' }).click();
  await form(page).getByLabel('Company').fill('Acme');
  await form(page).getByLabel('Vacancy text').fill('Some text');
  await form(page).getByRole('button', { name: 'Save vacancy' }).click();

  await expect(form(page)).toBeVisible();
  await expect(form(page).getByTestId('vacancy-title-error')).toBeVisible();
  await expect(form(page).getByLabel('Title')).toHaveAttribute('aria-invalid', 'true');

  await form(page).getByRole('button', { name: 'Cancel' }).click();
  await expect(list(page).getByTestId('vacancies-empty')).toBeVisible();
});

test("another user's vacancy is not shown in my list", async ({ page, request }) => {
  const owner = uniqueEmail();
  const other = uniqueEmail();
  await registerViaApi(request, owner);
  await registerViaApi(request, other);
  await createVacancyViaApi(request, owner, "A's secret role");

  await logIn(page, other);

  await expect(list(page).getByTestId('vacancies-empty')).toBeVisible();
  await expect(page.getByText("A's secret role")).toHaveCount(0);
});

test('a new user sees an empty state that links to pasting a vacancy', async ({ page, request }) => {
  const email = uniqueEmail();
  await registerViaApi(request, email);
  await logIn(page, email);

  await expect(list(page).getByTestId('vacancies-empty')).toBeVisible();
  await list(page).getByTestId('vacancies-empty').getByRole('link').click();

  await expect(form(page)).toBeVisible();
});
