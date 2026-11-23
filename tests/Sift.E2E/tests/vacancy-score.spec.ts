import { expect, test, type APIRequestContext, type Page } from '@playwright/test';
import type { components } from '../../../src/Sift.Web/src/api/generated/schema';

type Schemas = components['schemas'];
type RequirementKind = Schemas['RequirementKind'];
type MatchInput = Schemas['MatchInput'];
type CreateRequirementRequest = Schemas['CreateRequirementRequest'];
type CreateVacancyRequest = Schemas['CreateVacancyRequest'];
type SaveResumeRequest = Schemas['SaveResumeRequest'];
type RegisterRequest = Schemas['RegisterRequest'];

const apiUrl = 'http://localhost:5100';
const password = 'correct horse battery';

function uniqueEmail(): string {
  return `e2e-score-${Date.now()}-${Math.floor(Math.random() * 1_000_000)}@example.com`;
}

type AuthHeaders = { Authorization: string };

async function signUp(request: APIRequestContext, email: string): Promise<AuthHeaders> {
  const credentials: RegisterRequest = { email, password };
  const register = await request.post(`${apiUrl}/auth/register`, { data: credentials });
  expect(register.status()).toBe(201);
  const login = await request.post(`${apiUrl}/auth/login`, { data: credentials });
  expect(login.ok()).toBe(true);
  const { accessToken } = (await login.json()) as Schemas['LoginResponse'];

  return { Authorization: `Bearer ${accessToken}` };
}

async function saveResume(request: APIRequestContext, headers: AuthHeaders): Promise<void> {
  const body: SaveResumeRequest = { text: 'Ten years of backend work.' };
  const response = await request.put(`${apiUrl}/resume`, { headers, data: body });
  expect(response.ok()).toBe(true);
}

async function addRequirement(
  request: APIRequestContext,
  headers: AuthHeaders,
  text: string,
  kind: RequirementKind,
): Promise<string> {
  const body: CreateRequirementRequest = { text, kind };
  const response = await request.post(`${apiUrl}/requirements`, { headers, data: body });
  expect(response.status()).toBe(201);

  return ((await response.json()) as Schemas['Requirement']).id;
}

async function createVacancy(
  request: APIRequestContext,
  headers: AuthHeaders,
  title: string,
  rawText: string,
): Promise<string> {
  const body: CreateVacancyRequest = { title, company: 'Acme', rawText };
  const response = await request.post(`${apiUrl}/vacancies`, { headers, data: body });
  expect(response.status()).toBe(201);

  return ((await response.json()) as Schemas['Vacancy']).id;
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

const editorRegions: Record<MatchInput, string> = { resume: 'Resume', requirements: 'Requirements' };

const nothingToScoreAgainst: { state: string; hasResume: boolean; hasRequirements: boolean; missing: MatchInput[] }[] = [
  { state: 'no resume and no requirements', hasResume: false, hasRequirements: false, missing: ['resume', 'requirements'] },
  { state: 'requirements but no resume', hasResume: false, hasRequirements: true, missing: ['resume'] },
  { state: 'a resume but no requirements', hasResume: true, hasRequirements: false, missing: ['requirements'] },
];

for (const { state, hasResume, hasRequirements, missing } of nothingToScoreAgainst) {
  test(`with ${state} no score or verdicts are shown, only the missing inputs are named and each link reaches its editor`, async ({
    page,
    request,
  }) => {
    const email = uniqueEmail();
    const headers = await signUp(request, email);
    if (hasResume) {
      await saveResume(request, headers);
    }
    if (hasRequirements) {
      await addRequirement(request, headers, 'C#', 'must-have');
    }
    await createVacancy(request, headers, 'Unscored Engineer', 'We need C#');

    await logInAndOpen(page, email, 'Unscored Engineer');

    const notice = page.getByTestId('match-missing');
    await expect(notice).toContainText(`Missing: ${missing.join(', ')}.`);
    await expect(page.getByTestId('vacancy-score')).toHaveCount(0);
    await expect(page.getByTestId('verdict-item')).toHaveCount(0);

    const allInputs: MatchInput[] = ['resume', 'requirements'];
    for (const input of allInputs.filter((candidate) => !missing.includes(candidate))) {
      await expect(page.getByTestId(`match-missing-${input}`)).toHaveCount(0);
    }
    await expect(notice.getByRole('link')).toHaveCount(missing.length);

    for (const input of missing) {
      const link = page.getByTestId(`match-missing-${input}`);
      await expect(link).toHaveAttribute('href', `#/${input}`);

      await link.click();

      const editor = page.getByRole('region', { name: editorRegions[input] });
      await expect(editor).toBeVisible();
      await expect(page.getByTestId('vacancy-detail-title')).toHaveCount(0);

      await page.goBack();
      await expect(page.getByTestId('vacancy-detail-title')).toHaveText('Unscored Engineer');
    }
  });
}

test("only my own requirements are scored, another user's requirements never appear", async ({ page, request }) => {
  const otherHeaders = await signUp(request, uniqueEmail());
  await saveResume(request, otherHeaders);
  await addRequirement(request, otherHeaders, 'Kotlin', 'must-have');

  const email = uniqueEmail();
  const headers = await signUp(request, email);
  await saveResume(request, headers);
  await addRequirement(request, headers, 'C#', 'must-have');
  await createVacancy(request, headers, 'Shared Stack Engineer', 'We need C# and Kotlin');

  await logInAndOpen(page, email, 'Shared Stack Engineer');

  const panel = page.getByRole('region', { name: 'Score' });
  await expect(page.getByTestId('verdict-item')).toHaveCount(1);
  await expect(verdict(page, 'C#').getByTestId('verdict-outcome')).toHaveText('met');
  await expect(verdict(page, 'Kotlin')).toHaveCount(0);
  await expect(page.getByTestId('verdict-explanation').filter({ hasText: 'Kotlin' })).toHaveCount(0);
  await expect(panel).not.toContainText('Kotlin');
  await expect(page.getByTestId('vacancy-score')).toHaveText('100');
});

test("another user's resume does not count: I see a resume notice and no score or verdicts", async ({ page, request }) => {
  const otherHeaders = await signUp(request, uniqueEmail());
  await saveResume(request, otherHeaders);

  const email = uniqueEmail();
  const headers = await signUp(request, email);
  await addRequirement(request, headers, 'C#', 'must-have');
  await createVacancy(request, headers, 'Borrowed Resume Engineer', 'We need C#');

  await logInAndOpen(page, email, 'Borrowed Resume Engineer');

  await expect(page.getByTestId('match-missing')).toContainText('Missing: resume.');
  await expect(page.getByTestId('match-missing-resume')).toBeVisible();
  await expect(page.getByTestId('match-missing-requirements')).toHaveCount(0);
  await expect(page.getByTestId('vacancy-score')).toHaveCount(0);
  await expect(page.getByTestId('verdict-item')).toHaveCount(0);
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
