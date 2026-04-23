import { test, expect } from '@playwright/test';

// ─────────────────────────────────────────────────────────────────────────────
// E2E Tests — Dashboard Builder
// Chạy với: npx playwright test (cần dev server đang chạy tại localhost:5173)
// ─────────────────────────────────────────────────────────────────────────────

test.describe('Dashboard Builder', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('http://localhost:5173/dashboard');
  });

  test('sidebar hiển thị đúng tên plugins', async ({ page }) => {
    await expect(page.getByText('Tài Chính')).toBeVisible();
    await expect(page.getByText('Nhân Sự')).toBeVisible();
    await expect(page.getByText('Marketing')).toBeVisible();
  });

  test('có thể mở widget palette', async ({ page }) => {
    await page.getByRole('button', { name: '+ Thêm Widget' }).click();
    await expect(page.getByText('Widget Library')).toBeVisible();
  });

  test('có thể thêm widget vào dashboard', async ({ page }) => {
    await page.getByRole('button', { name: '+ Thêm Widget' }).click();
    // Click vào widget đầu tiên trong palette
    const firstWidget = page.locator('.ioc-palette__item').first();
    await firstWidget.click();
    // Widget xuất hiện trong drop zone
    await expect(page.locator('[data-drop-zone="main"] .ioc-widget')).toBeVisible();
  });

  test('có thể toggle edit mode', async ({ page }) => {
    await page.getByRole('button', { name: '✎ Chỉnh sửa' }).click();
    await expect(page.getByRole('button', { name: /Lưu Layout/ })).toBeVisible();
  });
});

test.describe('Navigation', () => {
  test('navigate đến Finance module', async ({ page }) => {
    await page.goto('http://localhost:5173');
    await page.getByText('Tài Chính').click();
    await expect(page).toHaveURL(/\/finance/);
  });

  test('navigate đến HR module', async ({ page }) => {
    await page.goto('http://localhost:5173');
    await page.getByText('Nhân Sự').click();
    await expect(page).toHaveURL(/\/hr/);
  });
});
