# AI Usage — Clinic Appointment Booking

## 1. AI tools used

I used **ChatGPT (OpenAI)** during this project, mainly for **learning and project planning**, as well as occasional implementation guidance and debugging. I had not previously worked with **C# or TypeScript**, so I used AI to understand the syntax, frameworks, and code I was writing rather than just obtain solutions.

## 2. What I asked

Most of my prompts were **explanation-based**. I frequently asked what a particular line or method did, why it was needed, and how it connected to the rest of the application. My questions generally fell into these areas:

- **Planning:** Breaking the project into small, achievable objectives, deciding the order of implementation, and starting with a basic project structure before adding features. I initially asked for a work breakdown without code so I could understand the overall task first.
- **Learning the technologies:** Understanding Angular, ASP.NET Core, Node.js, C#, and TypeScript, including controllers, routes, constructors, interfaces, Angular signals, and HTTP requests.
- **Connecting the application:** Understanding how the Angular frontend communicates with the .NET backend, what CORS and routing do, and how information moves from a user action to an API response.
- **Implementation and debugging:** Asking about booking validation, error responses, unit testing, and problems encountered while running the frontend or backend.

For example, I asked how a controller is defined, what `UseRouting()` and `UseCors()` do, why `ng serve` failed, and how the frontend and backend should be connected. These were usually follow-up questions to make sure I understood suggested code before proceeding.

## 3. What I kept, changed, or discarded, and why

**Kept:** I followed the general step-by-step development approach and used explanations of backend controllers, Angular state and HTTP calls, validation rules, and xUnit testing to guide the implementation. This helped me build the application in manageable stages and understand how its parts work together.

**Changed:** I often simplified suggestions to better suit the size of the project. For example, I kept the Angular HTTP calls in `app.ts` instead of introducing separate services immediately, and I used a doctor-selection table rather than a basic dropdown so more information was visible. I also adjusted the booking sequence to check availability before creating a patient record.

**Discarded or deferred:** I chose not to add some suggested complexity, such as additional service layers or a patient-deletion rollback workflow. More extensive refactoring remains a possible improvement.