import { chromium } from 'playwright';

const browser = await chromium.launch({ headless: true, args: ['--no-sandbox'] });
try {
  for (const width of [320, 375, 390, 430, 641, 700, 768, 900, 1024]) {
    const page = await browser.newPage({ viewport: { width, height: 800 }, deviceScaleFactor: 1 });
    await page.route('http://127.0.0.1:4200/runtime-config.js', route => route.fulfill({
      status: 200, contentType: 'application/javascript',
      body: 'window.JOURNEY_API_URL = "https://api.example.test";'
    }));
    await page.route('https://api.example.test/api/v1/**', async route => {
      const path = new URL(route.request().url()).pathname;
      const body = path === '/api/v1/auth/login' ? { accessToken: 'test-token', expiresAt: '2099-01-01T00:00:00Z' } :
        path === '/api/v1/users/me' ? { id: 'user-1', fullName: 'An', email: 'an@example.test' } : [];
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(body) });
    });
    await page.route('https://tile.openstreetmap.org/**', route => route.abort());
    await page.goto('http://127.0.0.1:4200/', { waitUntil: 'domcontentloaded' });
    if (width <= 640) await page.getByRole('button', { name: 'Cá nhân' }).click();
    else await page.locator('.account-link').click();
    await page.getByLabel('Email').fill('an@example.test');
    await page.getByLabel('Mật khẩu').fill('a-secure-password');
    await page.locator('.auth-form').getByRole('button', { name: 'Đăng nhập', exact: true }).click();
    await page.getByRole('button', { name: 'Đăng bài', exact: true }).last().click();
    await page.locator('.picker-map').waitFor();
    const dimensions = await page.evaluate(() => ({
      viewport: document.documentElement.clientWidth,
      page: document.documentElement.scrollWidth,
      form: document.querySelector('.post-form')?.getBoundingClientRect().width,
      map: document.querySelector('.picker-map')?.getBoundingClientRect().width,
      controls: [...document.querySelectorAll('.map-tools button, .mobile-nav button')]
        .map(button => button.getBoundingClientRect())
        .filter(rect => rect.width > 0)
        .map(rect => ({ width: rect.width, height: rect.height }))
    }));
    if (dimensions.page > dimensions.viewport + 1)
      throw new Error(`${width}px: horizontal overflow ${dimensions.page} > ${dimensions.viewport}`);
    if (!dimensions.form || !dimensions.map || dimensions.map > dimensions.form)
      throw new Error(`${width}px: form/map dimensions invalid: ${JSON.stringify(dimensions)}`);
    if (dimensions.controls.some(rect => rect.width < 44 || rect.height < 44))
      throw new Error(`${width}px: a touch control is smaller than 44px: ${JSON.stringify(dimensions.controls)}`);
    if (process.env.JOURNEY_SCREENSHOTS && [375, 700].includes(width))
      await page.screenshot({ path: `/tmp/journey-${width}.png`, fullPage: true });
    console.log(`${width}px: OK, page ${dimensions.page}px, map ${Math.round(dimensions.map)}px`);
    await page.close();
  }
} finally {
  await browser.close();
}
