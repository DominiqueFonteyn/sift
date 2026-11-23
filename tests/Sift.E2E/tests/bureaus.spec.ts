import { expect, test, type APIRequestContext, type Page } from '@playwright/test';

const apiUrl = 'http://localhost:5100';
const password = 'correct horse battery';

function uniqueEmail(): string {
  return `e2e-bureau-${Date.now()}-${Math.floor(Math.random() * 1_000_000)}@example.com`;
}

async function registerViaApi(request: APIRequestContext, email: string): Promise<string> {
  const response = await request.post(`${apiUrl}/auth/register`, { data: { email, password } });
  expect(response.status()).toBe(201);
  const login = await request.post(`${apiUrl}/auth/login`, { data: { email, password } });
  const { accessToken } = (await login.json()) as { accessToken: string };

  return accessToken;
}

async function logIn(page: Page, email: string): Promise<void> {
  await page.goto('/');
  const form = page.getByRole('region', { name: 'Login' });
  await form.getByLabel('Email').fill(email);
  await form.getByLabel('Password').fill(password);
  await form.getByRole('button', { name: 'Log in' }).click();
  await expect(page.getByTestId('signed-in')).toContainText(email);
  await page.getByRole('navigation', { name: 'Main navigation' }).getByRole('link', { name: 'Bureaus' }).click();
}

function section(page: Page) {
  return page.getByRole('region', { name: 'Bureaus', exact: true });
}

function form(page: Page) {
  return section(page).getByRole('form', { name: 'Add a bureau' });
}

test('a user adds a bureau with only a name and no website is shown', async ({ page, request }) => {
  const email = uniqueEmail();
  await registerViaApi(request, email);
  await logIn(page, email);
  await expect(section(page).getByTestId('bureaus-empty')).toBeVisible();

  await form(page).getByLabel('Name').fill('Acme Recruitment');
  await form(page).getByRole('button', { name: 'Add bureau' }).click();

  const rows = section(page).getByTestId('bureau-row');
  await expect(rows).toHaveCount(1);
  await expect(rows.first().getByTestId('bureau-name')).toHaveText('Acme Recruitment');
  await expect(rows.first().getByTestId('bureau-website')).toHaveCount(0);
  await expect(form(page).getByLabel('Name')).toHaveValue('');
});

test('a user opens a bureau and sees its name and website', async ({ page, request }) => {
  const email = uniqueEmail();
  await registerViaApi(request, email);
  await logIn(page, email);

  await form(page).getByLabel('Name').fill('Acme Recruitment');
  await form(page).getByLabel('Website').fill('https://acme.example');
  await form(page).getByRole('button', { name: 'Add bureau' }).click();
  const row = section(page).getByTestId('bureau-row');
  await expect(row.getByTestId('bureau-website')).toHaveText('https://acme.example');

  await row.getByTestId('bureau-name').click();

  const detail = page.getByRole('region', { name: 'Bureau', exact: true });
  await expect(detail.getByTestId('bureau-detail-name')).toHaveText('Acme Recruitment');
  await expect(detail.getByTestId('bureau-detail-website')).toHaveText('https://acme.example');

  await detail.getByTestId('bureau-back').click();
  await expect(section(page).getByTestId('bureau-row')).toHaveCount(1);
});

test('an empty name shows a name error and nothing is added', async ({ page, request }) => {
  const email = uniqueEmail();
  await registerViaApi(request, email);
  await logIn(page, email);

  await form(page).getByRole('button', { name: 'Add bureau' }).click();

  await expect(form(page).getByTestId('bureau-name-error')).toBeVisible();
  await expect(form(page).getByTestId('bureau-website-error')).toHaveCount(0);
  await expect(form(page).getByLabel('Name')).toHaveAttribute('aria-invalid', 'true');
  await expect(section(page).getByTestId('bureau-row')).toHaveCount(0);
  await expect(section(page).getByTestId('bureaus-empty')).toBeVisible();
});

test('an invalid website shows a website error and nothing is added', async ({ page, request }) => {
  const email = uniqueEmail();
  await registerViaApi(request, email);
  await logIn(page, email);

  await form(page).getByLabel('Name').fill('Acme Recruitment');
  await form(page).getByLabel('Website').fill('acme.example');
  await form(page).getByRole('button', { name: 'Add bureau' }).click();

  await expect(form(page).getByTestId('bureau-website-error')).toBeVisible();
  await expect(form(page).getByTestId('bureau-name-error')).toHaveCount(0);
  await expect(form(page).getByLabel('Website')).toHaveAttribute('aria-invalid', 'true');
  await expect(section(page).getByTestId('bureau-row')).toHaveCount(0);
  await expect(section(page).getByTestId('bureaus-empty')).toBeVisible();
});

test('a user with more than 20 bureaus can page to all of them', async ({ page, request }) => {
  const email = uniqueEmail();
  const token = await registerViaApi(request, email);
  for (let index = 1; index <= 21; index++) {
    const created = await request.post(`${apiUrl}/bureaus`, {
      headers: { Authorization: `Bearer ${token}` },
      data: { name: `Bureau ${String(index).padStart(2, '0')}` },
    });
    expect(created.status()).toBe(201);
  }

  await logIn(page, email);

  await expect(section(page).getByTestId('bureau-row')).toHaveCount(20);
  await expect(section(page).getByTestId('bureau-page-status')).toHaveText('Page 1 of 2');
  await section(page).getByRole('button', { name: 'Next' }).click();
  const rows = section(page).getByTestId('bureau-row');
  await expect(rows).toHaveCount(1);
  await expect(rows.first().getByTestId('bureau-name')).toHaveText('Bureau 21');
});

test("another user's bureau is not shown in my list", async ({ page, request }) => {
  const ownerToken = await registerViaApi(request, uniqueEmail());
  const other = uniqueEmail();
  await registerViaApi(request, other);
  const created = await request.post(`${apiUrl}/bureaus`, {
    headers: { Authorization: `Bearer ${ownerToken}` },
    data: { name: 'Alpha Search' },
  });
  expect(created.status()).toBe(201);

  await logIn(page, other);

  await expect(section(page).getByTestId('bureaus-empty')).toBeVisible();
  await expect(page.getByText('Alpha Search')).toHaveCount(0);
});
