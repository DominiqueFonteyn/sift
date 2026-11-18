import { expect, test } from '@playwright/test';

test('the shell reports the API health status', async ({ page }) => {
  await page.goto('/');

  await expect(page.getByRole('heading', { name: 'Sift' })).toBeVisible();
  await expect(page.getByTestId('health-status')).toHaveText(/API status:\s*healthy/);
});
