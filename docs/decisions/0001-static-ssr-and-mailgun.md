# 0001. Static server-side rendering and Mailgun for the contact form

**Date:** 2026-09-24
**Status:** Accepted (records existing design)

## Context

The site is a small brochure site for a single massage therapy practice. It needs fast page loads,
simple hosting, and a contact form that emails the therapist. There is no user login or database.

## Decision

- Use Blazor with **static server-side rendering** (`AddRazorComponents()` without an interactive
  render mode). Forms post back using `EditForm` with `Enhance`.
- Send contact form email through the **Mailgun HTTP API** using a typed `HttpClient`
  (`EmailSenderService`), with the API key stored in .NET user secrets locally and the host's
  secret store in production.

## Consequences

- Pages are plain HTML with no persistent SignalR connection: cheap to host and fast on mobile.
- Interactive behaviour (e.g. disabling a button while sending, live character counters) needs a
  small JavaScript snippet or opting a single component into `InteractiveServer`.
- Mailgun requires a verified sending domain for production delivery; the sandbox domain is only
  suitable for testing (see plan task 1.2).
- Secrets must never be committed; a previous leak required rewriting history (see CLAUDE.md).
