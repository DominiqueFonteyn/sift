import { expect, test, type APIRequestContext, type Page } from '@playwright/test';

const apiUrl = 'http://localhost:5100';
const password = 'correct horse battery';
const sessionKey = 'sift.session';

function uniqueEmail(): string {
  return `e2e-login-${Date.now()}-${Math.floor(Math.random() * 1_000_000)}@example.com`;
}

async function registerViaApi(request: APIRequestContext, email: string): Promise<void> {
  const response = await request.post(`${apiUrl}/auth/register`, { data: { email, password } });
  expect(response.status()).toBe(201);
}

async function submitLogin(page: Page, email: string, loginPassword: string): Promise<void> {
  const form = page.getByRole('region', { name: 'Login' });
  await form.getByLabel('Email').fill(email);
  await form.getByLabel('Password').fill(loginPassword);
  await form.getByRole('button', { name: 'Log in' }).click();
}

test('a registered user can log in and then log out', async ({ page, request }) => {
  const email = uniqueEmail();
  await registerViaApi(request, email);
  await page.goto('/');

  await submitLogin(page, email, password);

  await expect(page.getByTestId('signed-in')).toContainText(email);
  await expect(page.getByRole('region', { name: 'Login' })).toHaveCount(0);
  expect(await page.evaluate((key) => localStorage.getItem(key), sessionKey)).not.toBeNull();

  await page.reload();
  await expect(page.getByTestId('signed-in')).toContainText(email);

  await page.getByRole('button', { name: 'Log out' }).click();

  await expect(page.getByRole('region', { name: 'Login' })).toBeVisible();
  await expect(page.getByTestId('signed-in')).toHaveCount(0);
  expect(await page.evaluate((key) => localStorage.getItem(key), sessionKey)).toBeNull();

  await page.reload();
  await expect(page.getByRole('region', { name: 'Login' })).toBeVisible();
});

test('a wrong password and an unknown email show the same message', async ({ page, request }) => {
  const email = uniqueEmail();
  await registerViaApi(request, email);
  await page.goto('/');

  await submitLogin(page, email, 'not the password');
  const wrongPassword = await page.getByTestId('login-error').innerText();

  await submitLogin(page, uniqueEmail(), password);
  await expect(page.getByTestId('login-error')).toHaveText(wrongPassword);

  expect(wrongPassword).not.toMatch(/exist|unknown|not found/i);
  await expect(page.getByTestId('signed-in')).toHaveCount(0);
});

test('a token the API rejects sends the user back to the login screen', async ({ page }) => {
  await page.addInitScript(
    ([key, expiresAt]) => localStorage.setItem(key, JSON.stringify({ accessToken: 'not-a-valid-token', expiresAt })),
    [sessionKey, new Date(Date.now() + 60 * 60 * 1000).toISOString()],
  );

  await page.goto('/');

  await expect(page.getByRole('region', { name: 'Login' })).toBeVisible();
  await expect(page.getByTestId('session-expired')).toBeVisible();
  await expect(page.getByTestId('signed-in')).toHaveCount(0);
});

test('a stored token past its expiry is discarded on load', async ({ page }) => {
  await page.addInitScript(
    ([key, expiresAt]) => localStorage.setItem(key, JSON.stringify({ accessToken: 'stale', expiresAt })),
    [sessionKey, new Date(Date.now() - 60 * 1000).toISOString()],
  );

  await page.goto('/');

  await expect(page.getByRole('region', { name: 'Login' })).toBeVisible();
  await expect(page.getByTestId('session-expired')).toBeVisible();
});
