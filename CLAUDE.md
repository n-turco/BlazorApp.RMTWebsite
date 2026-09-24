# CLAUDE.md

Guidance for AI coding assistants (Claude Code and others) working in this repository.

## Project

Business website for Kinetic Flow Registered Massage Therapy (Nicholas Turco, RMT), built with
**.NET 10 Blazor** using **static server-side rendering** (no interactive render mode yet).
Pages: Home, Services, About, Contact (Mailgun email form), FAQ.

The improvement roadmap is `RMT_Website_Plan.docx` at the repo root — a local file that is gitignored
and never committed (tasks are numbered, e.g. "1.1", "2.3").
Reference those task numbers in branch names, commits, and PRs.

## Commands

Run from the repository root:

```bash
dotnet build                                  # build (must finish with 0 warnings, 0 errors)
dotnet run --project BlazorApp.RMTWebsite     # run locally (see launchSettings.json for URLs)
dotnet test                                   # run tests (once the test project exists — plan task 6.2)
dotnet format --verify-no-changes             # check formatting
```

## Layout

```
BlazorApp.RMTWebsite/
  Program.cs                 service registration and HTTP pipeline
  Components/App.razor       HTML shell (<head>, CSS/JS references)
  Components/Layout/         MainLayout (header/footer) and NavMenu
  Components/Pages/          one .razor file per route
  Models/                    form and content models (DataAnnotations validation)
  Services/                  EmailSenderService (Mailgun over HttpClient)
  wwwroot/                   static assets; app.css holds site-wide styles
docs/
  decisions/                 architecture decision records (ADRs)
  plans/                     feature plans written before larger changes
```

## Conventions

- **Styling:** use Bootstrap classes and component-scoped `*.razor.css` files. No inline `style=""`
  attributes. Brand colors go in CSS custom properties in `wwwroot/app.css`.
- **Markup:** exactly one `<h1>` per page; no `<br />` inside running text; valid HTML (no nested
  `<body>`, no headings inside `<p>`). Every page sets `<PageTitle>`.
- **Forms:** static SSR forms use `EditForm` with `FormName` and `Enhance`. `[SupplyParameterFromForm]`
  properties must be nullable with no initializer, initialized in `OnInitialized` (avoids BL0008).
- **Logging:** inject `ILogger<T>`; never use `Console.WriteLine`. Don't log visitor messages or
  personal details.
- **C#:** file-scoped namespaces for new files, nullable enabled, PascalCase properties,
  primary constructors for DI where they're already used.
- **Accessibility:** target WCAG 2.2 AA — labelled inputs, alt text, visible focus, 4.5:1 contrast.
- Keep changes small and focused; one plan task per branch/PR where practical.

## Secrets and configuration — important

A Mailgun API key was previously committed to this repo and had to be scrubbed from history.

- **Never** put API keys, passwords, or tokens in `appsettings*.json`, source code, docs, or commits.
- Local secrets live in .NET user secrets:
  `dotnet user-secrets set "Mailgun:ApiKey" "<key>" --project BlazorApp.RMTWebsite`
- Production secrets live in the host's configuration store.
- Do not read or print the user-secrets file. If a secret is needed for a task, ask the user.
- Config keys: `Mailgun:ApiKey` (secret), `Mailgun:BaseUrl`, `Mailgun:ToEmail`.

## Git workflow

- Default branch is `master`. Never commit directly to it; create a branch such as
  `feature/2.2-home-page`, `fix/1.1-reply-to`, or `chore/...`.
- Commit messages: imperative summary line (≤72 chars), blank line, short explanation of why.
- Before committing: `dotnet build` with no warnings, tests pass, and `git diff` checked for secrets.
- Never force-push `master` or rewrite shared history without explicit approval.

## Working with the assistant

- For anything larger than a small fix, write a short plan in `docs/plans/` first (use the template)
  and get agreement before coding.
- Record significant technical choices as an ADR in `docs/decisions/`.
- After UI changes, run the site and check the page at mobile (375px) and desktop widths.
