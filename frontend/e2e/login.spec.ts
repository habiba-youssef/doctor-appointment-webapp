
import { test, expect } from '@playwright/test';

test('Receptionist can log in successfully', async ({ page }) => {

	// ARRANGE: Get the receptionist password
 	const password = process.env['E2E_RECEPTIONIST_PASSWORD'];

	if (!password) {
		throw new Error('E2E_RECEPTIONIST_PASSWORD is not set.');
	}

	// STEP 1: Open the login page
	await page.goto('http://localhost:4200/login');

	// ASSERT: Check the login page is displayed
	await expect(
		page.getByRole('heading', { name: 'Clinic Login' })
	).toBeVisible();

	// STEP 2: Enter username and password
	await page.getByLabel('Username:').fill('receptionist');
	await page.getByLabel('Password:').fill(password);

  // STEP 3: Click Log In
	const loginResponsePromise = page.waitForResponse(
	response =>
		response.url().includes('/auth/login') &&
		response.request().method() === 'POST'
	);

	await page.getByRole('button', { name: 'Log In' }).click();

	const loginResponse = await loginResponsePromise;

	// Print the backend response status
	console.log('Login HTTP status:', loginResponse.status());

	// Print the error if login failed
	if (!loginResponse.ok()) {
	console.log('Login error:', await loginResponse.text());
	}


	// ASSERT 1: Check that the URL changed
	await expect(page).toHaveURL(
		'http://localhost:4200/receptionist'
	);

	// ASSERT 2: Check that the receptionist dashboard is displayed
	await expect(
		page.getByRole('heading', { name: 'Clinic Appointment Booking' })
	).toBeVisible();

});