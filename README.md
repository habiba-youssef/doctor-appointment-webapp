# Clinic Appointment Booking

A full-stack clinic appointment booking application built with **Angular 22** and **ASP.NET Core Web API (.NET 10)**. The application allows a receptionist to find patients, view doctors' availability, book, reschedule, and cancel appointments. Doctors can log in to view their own appointments and update appointment statuses.

This repository contains the original **Stage 1** application and its **Stage 2** improvements for a more production-ready system.

## Features

### Receptionist

- View doctors, their specialties, and consultation fees.
- Select an appointment type, doctor, and date to see available and unavailable time slots.
- Search for an existing patient by **12-digit Civil ID** and reuse their record, or register a new patient.
- Book a free appointment, reschedule to another doctor or time, or cancel with a reason.
- See current bookings and clear errors when the API rejects a request.

### Doctor

- Log in to view appointments assigned to their own doctor account.
- Select a date to see their schedule.
- Mark appointments as **Completed** or **No-show**.

### Stage 2 improvements

- **Persistent storage:** PostgreSQL with Entity Framework Core and migrations, replacing the original in-memory `DataStore`.
- **Concurrency protection:** PostgreSQL constraints prevent overlapping active appointments for the same doctor, including simultaneous requests.
- **Authentication and authorization:** JWT-based login with Receptionist and Doctor roles enforced by the API.
- **Kuwait time:** Booking and availability checks use `.NET TimeProvider` and `KuwaitTimeHelper` rather than the server's local clock.
- **Application structure:** Appointment rules are handled in `AppointmentsService`; Angular uses feature components and API services.
- **Docker Compose:** Runs the frontend, backend, and PostgreSQL together.
- **Testing:** Backend xUnit tests, a PostgreSQL/Testcontainers concurrency integration test, an Angular component test, and Playwright end-to-end tests.

## Technology stack

| Layer | Technology |
| --- | --- |
| Frontend | Angular 22, TypeScript, HTML, CSS |
| Backend | ASP.NET Core Web API, .NET 10, C# |
| Database | PostgreSQL 18, Entity Framework Core, Npgsql |
| Authentication | JWT Bearer authentication and role-based authorization |
| Containers | Docker, Docker Compose, Nginx (serves the Angular build) |
| Backend tests | xUnit, EF Core InMemory, WebApplicationFactory, Testcontainers for PostgreSQL |
| Frontend / E2E tests | Angular test runner / Vitest, Playwright |
| Automation | GitHub Actions (Playwright E2E workflow) |

## Prerequisites

For the main application, install:

- **Git**
- **Docker Desktop** (or Docker Engine with the Docker Compose plugin), with Docker running

To run tests locally outside the containers, also install **.NET 10 SDK**, **Node.js** compatible with Angular 22, and **npm**.

## Setup and run (Docker Compose)

Docker Compose builds and starts the **PostgreSQL database**, **ASP.NET Core API**, and **Angular frontend** together.

### 1. Clone the Stage 2 branch

```bash
git clone https://github.com/habiba-youssef/doctor-appointment-webapp.git
cd doctor-appointment-webapp
git switch stage-2
```

### 2. Configure environment variables (first run only)

Create a local `.env` file by copying `.env.example` from the repository root:

```powershell
Copy-Item .env.example .env
```

On macOS/Linux, use `cp .env.example .env` instead. Open `.env` and replace the example values:

| Variable | Purpose |
| --- | --- |
| `POSTGRES_PASSWORD` | Password for the PostgreSQL database user |
| `JWT_KEY` | Secret used to sign JWTs; at least 32 UTF-8 bytes |
| `RECEPTIONIST_PASSWORD` | Password for the seeded `receptionist` login |
| `DOCTOR_PASSWORD` | Password for the seeded `doctor` login |

Use your own local values; **do not commit `.env`**. The committed `.env.example` intentionally contains placeholders, not usable passwords.

### 3. Start the application

From the **repository root**:

```bash
docker compose up
```

On the first run, Docker builds the images. The backend applies EF Core migrations and seeds doctors, schedules, leave, and demo accounts at startup.

Open:

- **Frontend:** http://localhost:4200/login
- **API status:** http://localhost:5126/status — returns `Backend is working!`

**Note:** The `/status` endpoint checks whether the API responds; it does **not** currently verify database health.

To stop the application, press **Ctrl+C**, or run `docker compose down` in another terminal. PostgreSQL records persist in the named Docker volume `postgres_data` across normal restarts. **`docker compose down -v` removes the database volume and its data.**

## Seeded users

Two demonstration accounts are created automatically when the database is initialized:

| Username | Role | Permissions |
| --- | --- | --- |
| `receptionist` | Receptionist | Search/register patients; view, book, reschedule, and cancel appointments |
| `doctor` | Doctor | View only their own appointments and mark them Completed or No-show |

Passwords are taken from `RECEPTIONIST_PASSWORD` and `DOCTOR_PASSWORD` in `.env`. The seeded Doctor user is associated with the first doctor record (**Dr. Sarah Ahmed**). Passwords are hashed in the database; no actual login passwords are included in this README.

## How to use the application

**Receptionist workflow**

1. Log in with the `receptionist` account.
2. Select a doctor, an appointment type, and a date; then click **Check Availability**.
3. Choose an available time slot.
4. Search for the patient using their Civil ID. Use the existing record if found, or enter details to register a new patient.
5. Confirm the booking. The selected slot is checked again before the API creates the appointment.
6. Use the **Booked Appointments** list to reschedule or cancel. Cancellation requires a reason.

**Doctor workflow**

1. Log in with the `doctor` account.
2. View appointments for the selected day.
3. Mark a booking **Completed** or **No-show**, as appropriate.

## Booking rules

The backend validates these rules:

- **Appointment types:** Consultation = **30 minutes**; Follow-up = **15 minutes**; Procedure = **60 minutes**.
- **Start times:** Appointments start on a quarter-hour boundary (`:00`, `:15`, `:30`, or `:45`).
- **Clinic hours:** Sunday–Thursday, **08:00–16:00 Kuwait time**. The entire appointment must fit inside clinic hours and the selected doctor's own schedule.
- **Doctor schedules and leave:** Doctors have different weekly working hours and may have leave days when no bookings are allowed.
- **No overlaps:** A doctor cannot have overlapping active appointments. Booking validation also rejects a patient with an overlapping appointment at another doctor.
- **Past bookings:** Appointments cannot be created for times that have already passed in Kuwait.
- **Civil ID:** Each patient must have a unique 12-digit Civil ID.
- **Rescheduling:** A failed change leaves the original booking unchanged.
- **Cancellation:** A cancellation reason is required. An appointment that has already started cannot be cancelled. Cancelled appointments remain stored but no longer block availability.

Availability is calculated in **15-minute increments** and takes appointment duration, schedules, leave, past times, and existing bookings into account.

## API endpoints

Base URL: `http://localhost:5126`

| Method | Endpoint | Purpose |
| --- | --- | --- |
| `POST` | `/auth/login` | Log in and receive a JWT |
| `GET` | `/status` | Check that the API responds |
| `GET` | `/doctors` | List doctors |
| `GET` | `/patients` | List registered patients |
| `GET` | `/patients/search?civilId=...` | Find a patient by Civil ID |
| `POST` | `/patients` | Register a patient |
| `GET` | `/appointments` | List appointments; optional `doctorId` and `date` filters |
| `GET` | `/appointments/availability?doctorId=1&date=2030-01-06&type=0` | View availability for an appointment type |
| `POST` | `/appointments` | Book an appointment |
| `PUT` | `/appointments/{id}` | Reschedule an appointment |
| `DELETE` | `/appointments/{id}` | Cancel an appointment with a reason in the request body |
| `GET` | `/doctors/{doctorId}/appointments?date=2030-01-06` | Doctor's day view |
| `PUT` | `/doctors/{doctorId}/appointments/{appointmentId}/status` | Mark Completed or No-show |

Protected endpoints require an `Authorization: Bearer <token>` header. The API enforces role permissions, including preventing one Doctor from accessing another Doctor's appointments. Error responses use ASP.NET Core `ProblemDetails` or `ValidationProblemDetails`, with appropriate status codes such as **400**, **401**, **403**, **404**, and **409**.

## Testing

### Backend (xUnit)

From the repository root, with **.NET 10 SDK** installed and **Docker running**:

```bash
dotnet test tests/DoctorsAppointment.Api.Tests/DoctorsAppointment.Api.Tests.csproj
```

The backend tests include booking rules, Kuwait time handling, gender validation, duplicate Civil IDs, and a concurrency integration test. The `CLINIC101_TenParallelBookings_OnlyOneSucceeds` test uses a temporary PostgreSQL container through **Testcontainers**, starts the API with **WebApplicationFactory**, and expects **one `201 Created`** and **nine `409 Conflict`** responses for ten simultaneous requests to the same slot.

Other tests use EF Core's in-memory provider; they do not establish database-level concurrency guarantees.

### Angular component test

```bash
cd frontend
npm ci
npm test -- --watch=false
```

The existing `clinic105.spec.ts` test checks that a failed cancellation displays an error and refreshes the appointment list.

### Playwright end-to-end tests

The repository includes E2E tests for Receptionist login and booking. Run them against a **separate test database** rather than the regular development database.

First, stop the main Compose application if it is running (`docker compose down` from the repository root). Add these extra variables to the local `.env` file:

- `E2E_POSTGRES_PASSWORD`: password for the separate E2E PostgreSQL database
- `E2E_RECEPTIONIST_PASSWORD`: password for the seeded test receptionist

Keep `JWT_KEY` and `DOCTOR_PASSWORD` configured as above. Then, from the repository root:

```bash
docker compose -f compose.e2e.yaml up -d --build --wait
cd frontend
npm ci
npx playwright install chromium
npx playwright test e2e/login.spec.ts e2e/booking.spec.ts --project=chromium
```

After testing, return to the repository root and stop the E2E stack:

```bash
cd ..
docker compose -f compose.e2e.yaml down
```

On Windows, `reset-e2e.ps1` can reset the E2E database; it **deletes the E2E volume** and starts the test stack again.

### GitHub Actions

The current workflow is `.github/workflows/e2e.yml`. It runs the **Playwright Chromium login and booking tests** on pushes to `stage-2` and by manual dispatch, using repository secrets for test credentials. It does **not yet** run the complete backend and frontend test suites or automatically run on pull requests.

## Known limitations and future improvements

- **Complete the requested CI coverage:** Extend GitHub Actions to build both applications and run all backend, frontend, and integration tests on every push and pull request.
- **Improve the receptionist forms:** Replace the remaining browser-native `alert()`, `confirm()`, and `prompt()` dialogs with in-app dialogs and use reactive forms with field-level validation.
- **Add database-aware health checks:** `/status` only confirms that the API is responding; it does not check PostgreSQL connectivity.
- **Strengthen concurrency rules for patients:** Doctor overlaps are protected in PostgreSQL, while patient overlaps across different doctors are currently checked in application code without equivalent database-level protection.
- **Add further testing and operational safeguards:** More edge-case integration tests, structured event logging, and broader UI feedback would improve production readiness.