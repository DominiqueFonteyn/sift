import { expect, test, type APIRequestContext, type Page } from '@playwright/test';
import type { components } from '../../../src/Sift.Web/src/api/generated/schema';

type Schemas = components['schemas'];
type RequirementKind = Schemas['RequirementKind'];

const apiUrl = 'http://localhost:5100';
const password = 'correct horse battery';

function uniqueEmail(): string {
  return `e2e-rank-${Date.now()}-${Math.floor(Math.random() * 1_000_000)}@example.com`;
}

type AuthHeaders = { Authorization: string };

async function signUp(request: APIRequestContext, email: string): Promise<AuthHeaders> {
  const credentials: Schemas['RegisterRequest'] = { email, password };
  expect((await request.post(`${apiUrl}/auth/register`, { data: credentials })).status()).toBe(201);
  const login = await request.post(`${apiUrl}/auth/login`, { data: credentials });
  expect(login.ok()).toBe(true);
  const { accessToken } = (await login.json()) as Schemas['LoginResponse'];

  return { Authorization: `Bearer ${accessToken}` };
}

async function saveResume(request: APIRequestContext, headers: AuthHeaders): Promise<void> {
  const body: Schemas['SaveResumeRequest'] = { text: 'Ten years of backend work.' };
  expect((await request.put(`${apiUrl}/resume`, { headers, data: body })).ok()).toBe(true);
}

async function addRequirement(
  request: APIRequestContext,
  headers: AuthHeaders,
  text: string,
  kind: RequirementKind,
): Promise<void> {
  const body: Schemas['CreateRequirementRequest'] = { text, kind };
  expect((await request.post(`${apiUrl}/requirements`, { headers, data: body })).status()).toBe(201);
}

async function createVacancy(request: APIRequestContext, headers: AuthHeaders, title: string, rawText: string): Promise<void> {
  const body: Schemas['CreateVacancyRequest'] = { title, company: 'Acme', rawText };
  expect((await request.post(`${apiUrl}/vacancies`, { headers, data: body })).status()).toBe(201);
}

async function logIn(page: Page, email: string): Promise<void> {
  await page.goto('/');
  const form = page.getByRole('region', { name: 'Login' });
  await form.getByLabel('Email').fill(email);
  await form.getByLabel('Password').fill(password);
  await form.getByRole('button', { name: 'Log in' }).click();
  await expect(page.getByTestId('signed-in')).toContainText(email);
}

async function registerThroughTheForm(page: Page, email: string): Promise<void> {
  await page.goto('/');
  await page.getByRole('button', { name: 'Create an account' }).click();
  const form = page.getByRole('region', { name: 'Registration' });
  await form.getByLabel('Email').fill(email);
  await form.getByLabel('Password').fill(password);
  await form.getByRole('button', { name: 'Register' }).click();
  await expect(page.getByTestId('register-success')).toContainText(email);
  await page.getByRole('button', { name: 'Continue to log in' }).click();
}

async function logInThroughTheForm(page: Page, email: string): Promise<void> {
  const form = page.getByRole('region', { name: 'Login' });
  await form.getByLabel('Email').fill(email);
  await form.getByLabel('Password').fill(password);
  await form.getByRole('button', { name: 'Log in' }).click();
  await expect(page.getByTestId('signed-in')).toContainText(email);
}

async function openNav(page: Page, name: string): Promise<void> {
  await page.getByRole('navigation', { name: 'Main navigation' }).getByRole('link', { name }).click();
}

async function saveResumeThroughTheEditor(page: Page, text: string): Promise<void> {
  await openNav(page, 'Resume');
  const resume = page.getByRole('region', { name: 'Resume' });
  await resume.getByLabel('Resume text').fill(text);
  await resume.getByRole('button', { name: 'Save resume' }).click();
  await expect(page.getByTestId('resume-saved')).toBeVisible();
}

async function addRequirementThroughTheEditor(page: Page, text: string, kind: RequirementKind): Promise<void> {
  await openNav(page, 'Requirements');
  const section = page.getByRole('region', { name: 'Requirements' });
  await section.getByLabel('Requirement', { exact: true }).fill(text);
  await section.getByLabel('Kind').selectOption(kind);
  await section.getByRole('button', { name: 'Add requirement' }).click();
  await expect(section.getByTestId('requirement-item').filter({ hasText: text })).toBeVisible();
}

async function pasteVacancyThroughTheForm(page: Page, title: string, rawText: string, expectedRows: number): Promise<void> {
  await openNav(page, 'Vacancies');
  const list = page.getByRole('region', { name: 'Vacancies' });
  await list.getByRole('link', { name: 'Paste a vacancy' }).click();
  const form = page.getByRole('region', { name: 'New vacancy' });
  await form.getByLabel('Title').fill(title);
  await form.getByLabel('Company').fill('Acme');
  await form.getByLabel('Vacancy text').fill(rawText);
  await form.getByRole('button', { name: 'Save vacancy' }).click();
  await expect(list.getByTestId('vacancy-row')).toHaveCount(expectedRows);
}

function row(page: Page, title: string) {
  return page.getByTestId('vacancy-row').filter({ hasText: title });
}

test('MVP journey: register, resume, requirements, paste two vacancies, see them ranked and explained', async ({ page }) => {
  const email = uniqueEmail();
  await registerThroughTheForm(page, email);
  await logInThroughTheForm(page, email);
  await saveResumeThroughTheEditor(page, 'Ten years of backend work.');
  await addRequirementThroughTheEditor(page, 'C#', 'must-have');
  await addRequirementThroughTheEditor(page, 'Docker', 'nice-to-have');
  await pasteVacancyThroughTheForm(page, 'Backend role', 'We need C# and Docker', 1);
  await pasteVacancyThroughTheForm(page, 'Frontend role', 'We need Java', 2);

  await expect(page.getByTestId('vacancy-title')).toHaveText(['Backend role', 'Frontend role']);
  const backend = row(page, 'Backend role');
  await expect(backend.getByTestId('vacancy-row-score')).toBeVisible();
  await expect(backend.getByTestId('vacancy-row-explanation').first()).toContainText('"C#"');
  await expect(backend.getByTestId('vacancy-row-explanation').first()).toContainText('was found');
  await expect(backend.getByTestId('vacancy-must-have-failed')).toHaveCount(0);
  await expect(row(page, 'Frontend role').getByTestId('vacancy-must-have-failed')).toContainText('C#');
  await expect(page.getByTestId('vacancies-missing')).toHaveCount(0);
});

const unscoredStates: { name: string; resume: boolean; requirements: boolean; missing: ('resume' | 'requirements')[] }[] = [
  { name: 'a requirement and no resume', resume: false, requirements: true, missing: ['resume'] },
  { name: 'a resume and no requirements', resume: true, requirements: false, missing: ['requirements'] },
  { name: 'neither a resume nor requirements', resume: false, requirements: false, missing: ['resume', 'requirements'] },
];

for (const state of unscoredStates) {
  test(`with ${state.name} the list is newest first, unscored, and names exactly the missing inputs`, async ({ page, request }) => {
    const email = uniqueEmail();
    const headers = await signUp(request, email);
    if (state.resume) {
      await saveResume(request, headers);
    }

    if (state.requirements) {
      await addRequirement(request, headers, 'C#', 'must-have');
    }

    await createVacancy(request, headers, 'First role', 'We need C#');
    await createVacancy(request, headers, 'Second role', 'We need Java');
    await createVacancy(request, headers, 'Third role', 'We need C#');

    await logIn(page, email);

    await expect(page.getByTestId('vacancy-title')).toHaveText(['Third role', 'Second role', 'First role']);
    await expect(page.getByTestId('vacancy-row-score')).toHaveCount(0);
    await expect(page.getByTestId('vacancy-must-have-failed')).toHaveCount(0);
    const prompt = page.getByTestId('vacancies-missing');
    await expect(prompt.locator('p')).toHaveText(`Vacancies cannot be scored yet. Missing: ${state.missing.join(', ')}.`);
    for (const input of ['resume', 'requirements'] as const) {
      await expect(page.getByTestId(`vacancies-missing-${input}`)).toHaveCount(state.missing.includes(input) ? 1 : 0);
    }
  });
}
