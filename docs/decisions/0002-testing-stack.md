# 0002. Testing stack: xUnit v3, hand-written fakes, WebApplicationFactory, Playwright

**Date:** 2026-09-24
**Status:** Accepted

## Context

The site had no automated tests, and a real bug (the contact form email never includes the
visitor's address, plan task 1.1) went unnoticed. Phases 2–4 of the roadmap rework almost every
page, so changes need a safety net that runs in seconds, locally and in CI, without Mailgun
credentials or network access.

## Decision

- **xUnit v3** for all .NET tests, in one `BlazorApp.RMTWebsite.Tests` project whose folders mirror
  the app. On the .NET 10 SDK, xUnit v3 runs through **Microsoft.Testing.Platform**, enabled in the
  root `global.json`.
- **Hand-written fakes** instead of a mocking library to start with. `FakeHttpMessageHandler`
  replaces the network for `EmailSenderService`. Fakes are short, easy to read, and teach how the
  code under test really works. A mocking library (e.g. NSubstitute) can be added later if fakes multiply.
- **`Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`)** for integration tests that run the
  whole site in memory, with `IEmailSender` swapped for a fake (6.2 PR 2).
- **bUnit** only for components with real logic. Static SSR form posts are tested through
  integration tests instead (6.2 PR 2).
- **Playwright** for a small number of browser tests at mobile and desktop widths (6.2 PR 3).
- **coverlet** for coverage reports, aiming for about 80% on `Services` and `Models`, not 100%.

Alternatives: NUnit or MSTest would work equally well; xUnit is what most ASP.NET Core docs and
samples use. Selenium was passed over for Playwright, which is faster and has first-class .NET support.

## Consequences

- `dotnet test` runs everything with no secrets, so CI (6.3) can run it on every PR.
- Every bug fix and feature is expected to include tests (see CLAUDE.md, "Testing").
- `global.json` must stay. Without it, `dotnet test` fails with "Testing with VSTest target is no
  longer supported".
- Browser tests need Playwright's browsers installed (`pwsh bin/Debug/net10.0/playwright.ps1 install`),
  which is an extra CI step.
