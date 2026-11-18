import { expect, test, type Page } from '@playwright/test';

const validPassword = 'correct horse battery';

function uniqueEmail(): string {
  return `e2e-${Date.now()}-${Math.floor(Math.random() * 1_000_000)}@example.com`;
}

async function submitRegistration(page: Page, email: string, password: string): Promise<void> {
  const form = page.getByRole('region', { name: 'Registration' });
  await form.getByLabel('Email').fill(email);
  await form.getByLabel('Password').fill(password);
  await form.getByRole('button', { name: 'Register' }).click();
}

test('a visitor can register an account', async ({ page }) => {
  const email = uniqueEmail();
  await page.goto('/');

  await submitRegistration(page, email, validPassword);

  await expect(page.getByTestId('register-success')).toContainText(email);
});

test('registering the same email twice shows a conflict', async ({ page }) => {
  const email = uniqueEmail();
  await page.goto('/');
  await submitRegistration(page, email, validPassword);
  await expect(page.getByTestId('register-success')).toBeVisible();

  await page.goto('/');
  await submitRegistration(page, email, validPassword);

  await expect(page.getByTestId('register-error')).toContainText('already exists');
});

test('a password that is too short is reported on the password field', async ({ page }) => {
  await page.goto('/');

  await submitRegistration(page, uniqueEmail(), 'short');

  await expect(page.locator('#register-password-errors')).toContainText('at least 8 characters');
  await expect(page.getByRole('region', { name: 'Registration' }).getByLabel('Password')).toHaveAttribute(
    'aria-invalid',
    'true',
  );
  await expect(page.getByTestId('register-success')).toHaveCount(0);
});
