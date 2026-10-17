import { test, expect } from '@playwright/test';
import { API_URL } from '../config';

test('I can see sign in page', async ({ page }) => {
  await page.goto(API_URL);

  await expect(page.getByRole('button', { name: /使用 Google 登入/ })).toBeVisible();
});
