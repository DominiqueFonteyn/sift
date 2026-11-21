import { expect, test, type APIRequestContext, type Page } from '@playwright/test';

const apiUrl = 'http://localhost:5100';
const password = 'correct horse battery';

function uniqueEmail(): string {
  return `e2e-recruiter-${Date.now()}-${Math.floor(Math.random() * 1_000_000)}@example.com`;
}

async function registerViaApi(request: APIRequestContext, email: string): Promise<void> {
  const response = await request.post(`${apiUrl}/auth/register`, { data: { email, password } });
  expect(response.status()).toBe(201);
}

async function tokenFor(request: APIRequestContext, email: string): Promise<string> {
  const login = await request.post(`${apiUrl}/auth/login`, { data: { email, password } });
  const { accessToken } = (await login.json()) as { accessToken: string };

  return accessToken;
}

async function createBureauViaApi(request: APIRequestContext, token: string, name: string): Promise<void> {
  const created = await request.post(`${apiUrl}/bureaus`, { headers: { Authorization: `Bearer ${token}` }, data: { name } });
  expect(created.status()).toBe(201);
}

async function logIn(page: Page, email: string): Promise<void> {
  await page.goto('/');
  const form = page.getByRole('region', { name: 'Login' });
  await form.getByLabel('Email').fill(email);
  await form.getByLabel('Password').fill(password);
  await form.getByRole('button', { name: 'Log in' }).click();
  await expect(page.getByTestId('signed-in')).toContainText(email);
}

function section(page: Page) {
  return page.getByRole('region', { name: 'Recruiters' });
}

function form(page: Page) {
  return section(page).getByRole('form', { name: 'Add a recruiter' });
}

test('a user adds a recruiter with only a name', async ({ page, request }) => {
  const email = uniqueEmail();
  await registerViaApi(request, email);
  await logIn(page, email);
  await expect(section(page).getByTestId('recruiters-empty')).toBeVisible();

  await form(page).getByLabel('Name').fill('Jane Doe');
  await form(page).getByRole('button', { name: 'Add recruiter' }).click();

  const rows = section(page).getByTestId('recruiter-row');
  await expect(rows).toHaveCount(1);
  await expect(rows.first().getByTestId('recruiter-name')).toHaveText('Jane Doe');
  await expect(rows.first().getByTestId('recruiter-email')).toHaveCount(0);
  await expect(rows.first().getByTestId('recruiter-phone')).toHaveCount(0);
  await expect(rows.first().getByTestId('recruiter-linkedin')).toHaveCount(0);
  await expect(form(page).getByLabel('Name')).toHaveValue('');
});

test('a user adds a recruiter with every field', async ({ page, request }) => {
  const email = uniqueEmail();
  await registerViaApi(request, email);
  await logIn(page, email);

  await form(page).getByLabel('Name').fill('John Smith');
  await form(page).getByLabel('Email').fill('john@acme.example');
  await form(page).getByLabel('Phone').fill('+32 470 12 34 56');
  await form(page).getByLabel('LinkedIn URL').fill('https://www.linkedin.com/in/johnsmith');
  await form(page).getByRole('button', { name: 'Add recruiter' }).click();

  const row = section(page).getByTestId('recruiter-row');
  await expect(row).toHaveCount(1);
  await expect(row.getByTestId('recruiter-name')).toHaveText('John Smith');
  await expect(row.getByTestId('recruiter-email')).toHaveText('john@acme.example');
  await expect(row.getByTestId('recruiter-phone')).toHaveText('+32 470 12 34 56');
  await expect(row.getByTestId('recruiter-linkedin')).toHaveText('https://www.linkedin.com/in/johnsmith');
});

test('validation errors are shown next to the fields and nothing is added', async ({ page, request }) => {
  const email = uniqueEmail();
  await registerViaApi(request, email);
  await logIn(page, email);

  await form(page).getByLabel('Email').fill('not-an-email');
  await form(page).getByRole('button', { name: 'Add recruiter' }).click();

  await expect(form(page).getByTestId('recruiter-name-error')).toBeVisible();
  await expect(form(page).getByTestId('recruiter-email-error')).toBeVisible();
  await expect(form(page).getByLabel('Name')).toHaveAttribute('aria-invalid', 'true');
  await expect(form(page).getByLabel('Email')).toHaveAttribute('aria-invalid', 'true');
  await expect(section(page).getByTestId('recruiter-row')).toHaveCount(0);
  await expect(section(page).getByTestId('recruiters-empty')).toBeVisible();
});

function bureauPicker(page: Page) {
  return form(page).getByLabel('Bureau');
}

test('a user links a recruiter to a bureau', async ({ page, request }) => {
  const email = uniqueEmail();
  await registerViaApi(request, email);
  await createBureauViaApi(request, await tokenFor(request, email), 'Acme Recruitment');
  await logIn(page, email);

  await form(page).getByLabel('Name').fill('Jane Doe');
  await bureauPicker(page).selectOption({ label: 'Acme Recruitment' });
  await form(page).getByRole('button', { name: 'Add recruiter' }).click();

  const rows = section(page).getByTestId('recruiter-row');
  await expect(rows).toHaveCount(1);
  await expect(rows.first().getByTestId('recruiter-name')).toHaveText('Jane Doe');
  await expect(rows.first().getByTestId('recruiter-bureau')).toHaveText('Acme Recruitment');
});

test('the bureau defaults to "No bureau" and a recruiter can be added without one', async ({ page, request }) => {
  const email = uniqueEmail();
  await registerViaApi(request, email);
  await createBureauViaApi(request, await tokenFor(request, email), 'Acme Recruitment');
  await logIn(page, email);

  await expect(bureauPicker(page)).toHaveValue('');
  await expect(bureauPicker(page).locator('option:checked')).toHaveText('No bureau');

  await form(page).getByLabel('Name').fill('Sam Lee');
  await form(page).getByRole('button', { name: 'Add recruiter' }).click();

  const rows = section(page).getByTestId('recruiter-row');
  await expect(rows).toHaveCount(1);
  await expect(rows.first().getByTestId('recruiter-name')).toHaveText('Sam Lee');
  await expect(rows.first().getByTestId('recruiter-bureau')).toHaveCount(0);
});

test('the bureau choices offer only my own bureaus', async ({ page, request }) => {
  const owner = uniqueEmail();
  const other = uniqueEmail();
  await registerViaApi(request, owner);
  await registerViaApi(request, other);
  await createBureauViaApi(request, await tokenFor(request, owner), 'Acme Recruitment');
  await createBureauViaApi(request, await tokenFor(request, other), 'Beta Talent');
  await logIn(page, owner);

  const options = bureauPicker(page).locator('option');
  await expect(options).toHaveText(['No bureau', 'Acme Recruitment']);
});

test('a user without bureaus sees only "No bureau" and can still add a recruiter', async ({ page, request }) => {
  const email = uniqueEmail();
  await registerViaApi(request, email);
  await logIn(page, email);
  await expect(section(page).getByTestId('recruiters-empty')).toBeVisible();

  await expect(bureauPicker(page).locator('option')).toHaveText(['No bureau']);
  await form(page).getByLabel('Name').fill('Freelance Fran');
  await form(page).getByRole('button', { name: 'Add recruiter' }).click();

  const rows = section(page).getByTestId('recruiter-row');
  await expect(rows).toHaveCount(1);
  await expect(rows.first().getByTestId('recruiter-bureau')).toHaveCount(0);
});

test('the bureau choices include every bureau, beyond the first page of 100', async ({ page, request }) => {
  test.setTimeout(120_000);
  const email = uniqueEmail();
  await registerViaApi(request, email);
  const token = await tokenFor(request, email);
  for (let index = 1; index <= 101; index++) {
    await createBureauViaApi(request, token, `Bureau ${String(index).padStart(3, '0')}`);
  }
  await logIn(page, email);

  await expect(bureauPicker(page).locator('option')).toHaveCount(102);
  await form(page).getByLabel('Name').fill('Late Larry');
  await bureauPicker(page).selectOption({ label: 'Bureau 101' });
  await form(page).getByRole('button', { name: 'Add recruiter' }).click();

  await expect(section(page).getByTestId('recruiter-bureau')).toHaveText('Bureau 101');
});

test("another user's recruiter is not shown in my list", async ({ page, request }) => {
  const owner = uniqueEmail();
  const other = uniqueEmail();
  await registerViaApi(request, owner);
  await registerViaApi(request, other);
  const login = await request.post(`${apiUrl}/auth/login`, { data: { email: owner, password } });
  const { accessToken } = (await login.json()) as { accessToken: string };
  const created = await request.post(`${apiUrl}/recruiters`, {
    headers: { Authorization: `Bearer ${accessToken}` },
    data: { name: "A's private recruiter" },
  });
  expect(created.status()).toBe(201);

  await logIn(page, other);

  await expect(section(page).getByTestId('recruiters-empty')).toBeVisible();
  await expect(page.getByText("A's private recruiter")).toHaveCount(0);
});
