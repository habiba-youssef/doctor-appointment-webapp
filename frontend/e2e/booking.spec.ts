
import { test, expect } from '@playwright/test';

test('Receptionist can book an appointment', async ({ page }) => {

  // ARRANGE: Get login password
  const password = process.env['E2E_RECEPTIONIST_PASSWORD'];

  if (!password) {
    throw new Error('E2E_RECEPTIONIST_PASSWORD is not set.');
  }

  // STEP 1: Log in as Receptionist
  await page.goto('http://localhost:4200/login');

  await page.getByLabel('Username:').fill('receptionist');
  await page.getByLabel('Password:').fill(password);
  await page.getByRole('button', { name: 'Log In' }).click();

  await expect(page).toHaveURL('http://localhost:4200/receptionist');

  await expect(
    page.getByRole('heading', { name: 'Available Doctors' })
  ).toBeVisible();


  // STEP 2: Select Dr. Sarah Ahmed
  const doctorRow = page.getByRole('row')
    .filter({ hasText: 'Dr. Sarah Ahmed' });

  await doctorRow.getByRole('button', { name: 'Select' }).click();


  // STEP 3: Choose a future Monday
  // Kuwait is UTC+3. Dr. Sarah Ahmed works Mondays.
  const kuwaitNow = new Date(Date.now() + 3 * 60 * 60 * 1000);

  const daysUntilMonday =
    ((8 - kuwaitNow.getUTCDay()) % 7) + 7;

  const bookingDate = new Date(Date.UTC(
    kuwaitNow.getUTCFullYear(),
    kuwaitNow.getUTCMonth(),
    kuwaitNow.getUTCDate() + daysUntilMonday
  )).toISOString().slice(0, 10);

  await page.locator('#date').fill(bookingDate);
  await page.locator('#date').press('Tab');

  // STEP 4: Check available appointment slots
  await page.getByRole('button', {
    name: 'Check Availability',
    exact: true
  }).click();

  // Choose the first enabled (available) slot
  const availableSlot = page.locator('.slotsButton button:enabled').first();

  await expect(availableSlot).toBeVisible();

  const bookingTime = (await availableSlot.innerText()).slice(0, 5);

  await availableSlot.click();


  // STEP 5: Search for a new patient
  // Generate a unique 12-digit Civil ID for this test
  const civilId = Date.now().toString().slice(-12);
  const lastName = `Test${civilId.slice(-6)}`;

  await page.locator('#civilId').fill(civilId);

  await page.getByRole('button', {
    name: 'Search',
    exact: true
  }).click();

  await expect(
    page.getByText('Patient not found. Please register.')
  ).toBeVisible();


  // STEP 6: Enter patient information
  await page.locator('#firstName').fill('Playwright');
  await page.locator('#lastName').fill(lastName);
  await page.locator('#phone').fill('55550000');
  await page.locator('#gender').selectOption('1');
  await page.locator('#dob').fill('2000-01-01');


  // STEP 7: Handle confirmation and success popups
  page.on('dialog', async dialog => {
    await dialog.accept();
  });

  // Watch for the booking API response
  const bookingResponsePromise = page.waitForResponse(
    response =>
      response.url().endsWith('/appointments') &&
      response.request().method() === 'POST'
  );

  // Submit the appointment
  await page.getByRole('button', {
    name: 'Book Appointment'
  }).click();

  const bookingResponse = await bookingResponsePromise;


  // ASSERT 1: Backend successfully created appointment
  expect(bookingResponse.status()).toBe(201);


  // ASSERT 2: Appointment appears in the dashboard
  const bookingsSection = page.locator('section').filter({
    has: page.getByRole('heading', { name: 'Booked Appointments' })
  });

  const bookedRow = bookingsSection.getByRole('row')
    .filter({ hasText: `Playwright ${lastName}` });

  await expect(bookedRow).toBeVisible();

  // ASSERT 3: Correct doctor, date and time
  await expect(bookedRow).toContainText('Dr. Sarah Ahmed');
  await expect(bookedRow).toContainText(bookingDate);
  await expect(bookedRow).toContainText(bookingTime);

});
