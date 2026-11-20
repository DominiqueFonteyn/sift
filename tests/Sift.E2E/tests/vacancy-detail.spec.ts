import { expect, test, type APIRequestContext, type Page } from '@playwright/test';

const apiUrl = 'http://localhost:5100';
const password = 'correct horse battery';
const sessionKey = 'sift.session';
const timezoneId = 'America/New_York';
const nearMidnightUtc = '2026-03-10T00:30:00Z';

test.use({ timezoneId, locale: 'en-US' });

function uniqueEmail(): string {
  return `e2e-detail-${Date.now()}-${Math.floor(Math.random() * 1_000_000)}@example.com`;
}

async function registerViaApi(request: APIRequestContext, email: string): Promise<void> {
  const response = await request.post(`${apiUrl}/auth/register`, { data: { email, password } });
  expect(response.status()).toBe(201);
}

async function logIn(page: Page, email: string): Promise<void> {
  await page.goto('/');
  await submitLogin(page, email);
  await expect(page.getByTestId('signed-in')).toContainText(email);
}

async function submitLogin(page: Page, email: string): Promise<void> {
  const form = page.getByRole('region', { name: 'Login' });
  await form.getByLabel('Email').fill(email);
  await form.getByLabel('Password').fill(password);
  await form.getByRole('button', { name: 'Log in' }).click();
}

async function createVacancyViaApi(
  request: APIRequestContext,
  email: string,
  vacancy: { title: string; company: string; rawText: string },
): Promise<string> {
  const login = await request.post(`${apiUrl}/auth/login`, { data: { email, password } });
  expect(login.ok()).toBe(true);
  const { accessToken } = (await login.json()) as { accessToken: string };

  const response = await request.post(`${apiUrl}/vacancies`, {
    headers: { Authorization: `Bearer ${accessToken}` },
    data: vacancy,
  });
  expect(response.status()).toBe(201);
  return ((await response.json()) as { id: string }).id;
}

function list(page: Page) {
  return page.getByRole('region', { name: 'Vacancies' });
}

async function expectDetail(page: Page, vacancy: { title: string; company: string; rawText: string }): Promise<void> {
  await expect(page.getByTestId('vacancy-detail-title')).toHaveText(vacancy.title);
  await expect(page.getByTestId('vacancy-detail-company')).toHaveText(vacancy.company);
  // textContent, because toHaveText would normalise the whitespace this test is about.
  await expect(page.getByTestId('vacancy-detail-text')).toBeVisible();
  expect(await page.getByTestId('vacancy-detail-text').evaluate((element) => element.textContent)).toBe(vacancy.rawText);
}

async function expectNotFound(page: Page, secrets: string[] = []): Promise<void> {
  await expect(page.getByTestId('vacancy-not-found')).toBeVisible();
  await expect(page.getByTestId('vacancy-detail-title')).toHaveCount(0);
  for (const secret of secrets) {
    await expect(page.getByText(secret)).toHaveCount(0);
  }
}

// Longer than anything a list row could show, with leading/trailing whitespace and blank lines.
const longText = `  Line one\n\n  Line two  \n${'We need C# and SQL and a lot more. '.repeat(120)}\nLast line  \n`;

test('a user opens a vacancy from the list, reloads it, opens it directly and goes back', async ({
  page,
  request,
  context,
}) => {
  const email = uniqueEmail();
  await registerViaApi(request, email);
  const vacancy = { title: 'Backend Engineer', company: 'Acme', rawText: longText };
  const id = await createVacancyViaApi(request, email, vacancy);
  await logIn(page, email);

  await list(page).getByRole('link', { name: 'Backend Engineer' }).click();

  await expect(page).toHaveURL(new RegExp(`${id}$`));
  await expectDetail(page, vacancy);

  await page.reload();
  await expect(page.getByTestId('signed-in')).toBeVisible();
  await expectDetail(page, vacancy);

  const direct = await context.newPage();
  await direct.goto(`/#/vacancies/${id}`);
  await expectDetail(direct, vacancy);
  await direct.close();

  await page.getByTestId('vacancy-back').click();
  await expect(list(page)).toBeVisible();
  await expect(list(page).getByTestId('vacancy-row')).toHaveCount(1);
});

test('a vacancy created near midnight UTC shows the same local date in the list and on its page', async ({
  page,
  request,
}) => {
  const email = uniqueEmail();
  await registerViaApi(request, email);
  const id = await createVacancyViaApi(request, email, { title: 'Night Owl', company: 'Acme', rawText: 'text' });

  // The API clock cannot be set from here, so the browser is served the creation time under test.
  await page.route(/\/vacancies(\?.*)?$/, async (route) => {
    const response = await route.fetch();
    const body = (await response.json()) as { items: { createdAt: string }[] };
    body.items.forEach((item) => (item.createdAt = nearMidnightUtc));
    await route.fulfill({ response, json: body });
  });
  await page.route(`**/vacancies/${id}`, async (route) => {
    const response = await route.fetch();
    const body = (await response.json()) as { createdAt: string };
    body.createdAt = nearMidnightUtc;
    await route.fulfill({ response, json: body });
  });

  await logIn(page, email);
  const expected = new Date(nearMidnightUtc).toLocaleDateString('en-US', { timeZone: timezoneId });
  expect(expected).toBe('3/9/2026');
  await expect(list(page).getByTestId('vacancy-created')).toHaveText(expected);

  await list(page).getByRole('link', { name: 'Night Owl' }).click();

  await expect(page.getByTestId('vacancy-detail-created')).toHaveText(expected);
});

test("another user's vacancy, an unknown id and a malformed id all show the same not-found message", async ({
  page,
  request,
}) => {
  const owner = uniqueEmail();
  const other = uniqueEmail();
  await registerViaApi(request, owner);
  await registerViaApi(request, other);
  const secrets = ['Secret Title Zebra', 'Secret Company Yak', 'Secret text Walrus'];
  const id = await createVacancyViaApi(request, owner, {
    title: secrets[0],
    company: secrets[1],
    rawText: secrets[2],
  });
  await logIn(page, other);

  await page.goto(`/#/vacancies/${id}`);
  await expectNotFound(page, secrets);
  const foreignMessage = await page.getByTestId('vacancy-not-found').innerText();

  await page.goto('/#/vacancies/3f2b8c1e-7d4a-4c1e-9b0a-5a6d7e8f9a0b');
  await expectNotFound(page, secrets);
  await expect(page.getByTestId('vacancy-not-found')).toHaveText(foreignMessage);

  await page.goto('/#/vacancies/not-a-uuid');
  await expectNotFound(page, secrets);
  await expect(page.getByTestId('vacancy-not-found')).toHaveText(foreignMessage);

  await page.getByTestId('vacancy-back').click();
  await expect(list(page).getByTestId('vacancies-empty')).toBeVisible();
});

test('an expired session on a vacancy page shows the login screen with the notice, then returns to the vacancy', async ({
  page,
  request,
}) => {
  const email = uniqueEmail();
  await registerViaApi(request, email);
  const vacancy = { title: 'Backend Engineer', company: 'Acme', rawText: 'Private text Walrus' };
  const id = await createVacancyViaApi(request, email, vacancy);
  await page.addInitScript(
    ([key, expiresAt]) => localStorage.setItem(key, JSON.stringify({ accessToken: 'not-a-valid-token', expiresAt })),
    [sessionKey, new Date(Date.now() + 60 * 60 * 1000).toISOString()],
  );

  await page.goto(`/#/vacancies/${id}`);

  await expect(page.getByRole('region', { name: 'Login' })).toBeVisible();
  await expect(page.getByTestId('session-expired')).toBeVisible();
  await expect(page.getByText('Walrus')).toHaveCount(0);
  await expect(page.getByTestId('vacancy-detail-title')).toHaveCount(0);

  await submitLogin(page, email);

  await expect(page).toHaveURL(new RegExp(`${id}$`));
  await expectDetail(page, vacancy);
});
