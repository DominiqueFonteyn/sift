import { expect, test, type APIRequestContext, type Page } from '@playwright/test';

const apiUrl = 'http://localhost:5100';
const password = 'correct horse battery';

type Kind = 'must-have' | 'nice-to-have';

function uniqueEmail(): string {
  return `e2e-score-${Date.now()}-${Math.floor(Math.random() * 1_000_000)}@example.com`;
}

async function signUp(request: APIRequestContext, email: string): Promise<{ Authorization: string }> {
  const register = await request.post(`${apiUrl}/auth/register`, { data: { email, password } });
  expect(register.status()).toBe(201);
  const login = await request.post(`${apiUrl}/auth/login`, { data: { email, password } });
  expect(login.ok()).toBe(true);
  const { accessToken } = (await login.json()) as { accessToken: string };

  return { Authorization: `Bearer ${accessToken}` };
}

async function saveResume(request: APIRequestContext, headers: { Authorization: string }): Promise<void> {
  const response = await request.put(`${apiUrl}/resume`, { headers, data: { text: 'Ten years of backend work.' } });
  expect(response.ok()).toBe(true);
}

async function addRequirement(
  request: APIRequestContext,
  headers: { Authorization: string },
  text: string,
  kind: Kind,
): Promise<string> {
  const response = await request.post(`${apiUrl}/requirements`, { headers, data: { text, kind } });
  expect(response.status()).toBe(201);

  return ((await response.json()) as { id: string }).id;
}

async function createVacancy(
  request: APIRequestContext,
  headers: { Authorization: string },
  title: string,
  rawText: string,
): Promise<string> {
  const response = await request.post(`${apiUrl}/vacancies`, { headers, data: { title, company: 'Acme', rawText } });
  expect(response.status()).toBe(201);

  return ((await response.json()) as { id: string }).id;
}

async function logInAndOpen(page: Page, email: string, title: string): Promise<void> {
  await page.goto('/');
  const form = page.getByRole('region', { name: 'Login' });
  await form.getByLabel('Email').fill(email);
  await form.getByLabel('Password').fill(password);
  await form.getByRole('button', { name: 'Log in' }).click();
  await expect(page.getByTestId('signed-in')).toContainText(email);
  await page.getByRole('region', { name: 'Vacancies' }).getByRole('link', { name: title }).click();
  await expect(page.getByTestId('vacancy-detail-title')).toHaveText(title);
}

function verdict(page: Page, requirement: string) {
  return page.getByTestId('verdict-item').and(page.locator(`[data-requirement="${requirement}"]`));
}

test('opening a vacancy shows an integer score and a verdict with an explanation for each requirement', async ({
  page,
  request,
}) => {
  const email = uniqueEmail();
  const headers = await signUp(request, email);
  await saveResume(request, headers);
  await addRequirement(request, headers, 'C#', 'must-have');
  await addRequirement(request, headers, 'Docker', 'nice-to-have');
  await createVacancy(request, headers, 'Backend Engineer', 'We need C# and SQL');

  await logInAndOpen(page, email, 'Backend Engineer');

  await expect(page.getByTestId('vacancy-score')).toHaveText(/^(\d|[1-9]\d|100)$/);
  await expect(page.getByTestId('match-missing')).toHaveCount(0);
  await expect(page.getByTestId('verdict-item')).toHaveCount(2);

  const csharp = verdict(page, 'C#');
  await expect(csharp.getByTestId('verdict-outcome')).toHaveText('met');
  await expect(csharp.getByTestId('verdict-kind')).toHaveText('Must-have');
  await expect(csharp.getByTestId('verdict-explanation')).toContainText('"C#"');
  await expect(csharp.getByTestId('verdict-explanation')).toContainText('was found');

  const docker = verdict(page, 'Docker');
  await expect(docker.getByTestId('verdict-outcome')).toHaveText('not met');
  await expect(docker.getByTestId('verdict-kind')).toHaveText('Nice-to-have');
  await expect(docker.getByTestId('verdict-explanation')).toContainText('"Docker"');
  await expect(docker.getByTestId('verdict-explanation')).toContainText('was not found');
});

test('an unmet must-have caps the score at 49 and is listed first, marked as a must-have', async ({ page, request }) => {
  const email = uniqueEmail();
  const headers = await signUp(request, email);
  await saveResume(request, headers);
  await addRequirement(request, headers, 'C#', 'must-have');
  await addRequirement(request, headers, 'Remote', 'must-have');
  await addRequirement(request, headers, 'SQL', 'nice-to-have');
  await addRequirement(request, headers, 'Docker', 'nice-to-have');
  await createVacancy(request, headers, 'On-site Engineer', 'C# SQL Docker, on-site only');

  await logInAndOpen(page, email, 'On-site Engineer');

  const score = Number(await page.getByTestId('vacancy-score').innerText());
  expect(score).toBeLessThanOrEqual(49);
  const first = page.getByTestId('verdict-item').first();
  await expect(first).toHaveAttribute('data-requirement', 'Remote');
  await expect(first.getByTestId('verdict-kind')).toHaveText('Must-have');
  await expect(first.getByTestId('verdict-outcome')).toHaveText('not met');
});

test('meeting every must-have scores at least 50', async ({ page, request }) => {
  const email = uniqueEmail();
  const headers = await signUp(request, email);
  await saveResume(request, headers);
  await addRequirement(request, headers, 'C#', 'must-have');
  await addRequirement(request, headers, 'Docker', 'nice-to-have');
  await createVacancy(request, headers, 'Passing Engineer', 'We need C#');

  await logInAndOpen(page, email, 'Passing Engineer');

  expect(Number(await page.getByTestId('vacancy-score').innerText())).toBeGreaterThanOrEqual(50);
});

test('without a resume or requirements no score is shown and each missing input is a link', async ({
  page,
  request,
}) => {
  const email = uniqueEmail();
  const headers = await signUp(request, email);
  await createVacancy(request, headers, 'Unscored Engineer', 'We need C#');

  await logInAndOpen(page, email, 'Unscored Engineer');

  const notice = page.getByTestId('match-missing');
  await expect(notice).toContainText('resume');
  await expect(notice).toContainText('requirements');
  await expect(page.getByTestId('vacancy-score')).toHaveCount(0);
  await expect(page.getByTestId('verdict-item')).toHaveCount(0);
  await expect(page.getByTestId('match-missing-resume')).toHaveAttribute('href', '#resume');
  await expect(page.getByTestId('match-missing-requirements')).toHaveAttribute('href', '#requirements');

  await page.getByTestId('match-missing-resume').click();
  await expect(page.getByRole('region', { name: 'Resume' })).toBeFocused();
  await expect(page.getByTestId('vacancy-detail-title')).toBeVisible();
});

test('only the missing input is named when the resume exists but there are no requirements', async ({
  page,
  request,
}) => {
  const email = uniqueEmail();
  const headers = await signUp(request, email);
  await saveResume(request, headers);
  await createVacancy(request, headers, 'Half Ready Engineer', 'We need C#');

  await logInAndOpen(page, email, 'Half Ready Engineer');

  await expect(page.getByTestId('match-missing')).toBeVisible();
  await expect(page.getByTestId('match-missing-requirements')).toBeVisible();
  await expect(page.getByTestId('match-missing-resume')).toHaveCount(0);
  await expect(page.getByTestId('vacancy-score')).toHaveCount(0);
});

test('replacing a requirement re-scores the vacancy the next time it is opened', async ({ page, request }) => {
  const email = uniqueEmail();
  const headers = await signUp(request, email);
  await saveResume(request, headers);
  const javaId = await addRequirement(request, headers, 'Java', 'must-have');
  await createVacancy(request, headers, 'Rescored Engineer', 'We need C#');

  await logInAndOpen(page, email, 'Rescored Engineer');
  await expect(verdict(page, 'Java').getByTestId('verdict-outcome')).toHaveText('not met');

  const removed = await request.delete(`${apiUrl}/requirements/${javaId}`, { headers });
  expect(removed.status()).toBe(204);
  await addRequirement(request, headers, 'C#', 'must-have');
  await page.reload();

  await expect(page.getByTestId('verdict-item')).toHaveCount(1);
  await expect(verdict(page, 'C#').getByTestId('verdict-outcome')).toHaveText('met');
  await expect(verdict(page, 'Java')).toHaveCount(0);
  expect(Number(await page.getByTestId('vacancy-score').innerText())).toBeGreaterThanOrEqual(50);
});
