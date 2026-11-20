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
