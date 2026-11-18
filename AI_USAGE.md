## Stage 2: Production Readiness

### 1. AI Tools Used

I continued using **ChatGPT (OpenAI)** throughout Stage 2, mainly to help me learn and understand new concepts, troubleshoot problems, and guide me through unfamiliar development tools.

Since Stage 2 introduced more advanced requirements, I used AI to explain concepts I had not worked with before, understand why certain approaches were needed, and help me implement them correctly.

### 2. What I Asked

Most of my questions remained **explanation-based**, as I wanted to understand the reasoning behind the code and the technologies I was using.

My questions mainly focused on:

- **Understanding new concepts:** Learning about race conditions, database concurrency, Entity Framework Core, migrations, dependency injection, authentication, authorization, and other concepts introduced in Stage 2. I frequently asked for explanations of how these worked and why they were necessary.
- **Docker setup:** Understanding what Docker, containers, Dockerfiles, and Docker Compose are, how they work together, and how to configure the frontend, backend, and database to run together.
- **Commands and development tools:** Asking for guidance on terminal commands, including Docker commands, database migrations, running tests, and troubleshooting errors. I often asked what specific commands did before executing them.
- **Code understanding and debugging:** Asking for explanations of unfamiliar C# and TypeScript syntax, understanding error messages, and identifying why certain parts of the application were not behaving as expected.
- **Testing:** Understanding the differences between unit, integration, and end-to-end testing, how testing tools work, and how to test more complex scenarios such as simultaneous booking requests.

I often asked follow-up questions to simplify explanations or go through individual steps in more detail until I understood how they related to my application.

### 3. What I Kept, Changed, or Discarded, and Why

**Kept:** I used AI explanations to improve my understanding of unfamiliar technologies and development concepts. I also followed step-by-step guidance when setting up Docker, configuring the development environment, and learning the commands needed to build, run, and test the application.

**Changed:** I adapted suggestions to match my existing project structure and implementation. Rather than applying every suggested solution directly, I worked through the logic, asked questions where something was unclear, and adjusted the approach as I learned more. For example, I moved from the original in-memory `DataStore` and application-level locking to PostgreSQL and database-level concurrency protection.

**Discarded or deferred:** I avoided adding unnecessary complexity when a simpler approach was sufficient for the assessment. Where suggestions involved additional features or approaches beyond the immediate requirements, I focused on understanding and implementing the core functionality first.

Overall, I used AI primarily as a **learning and troubleshooting resource**, helping me understand the concepts, tools, commands, and implementation decisions involved in developing a more production-ready application.