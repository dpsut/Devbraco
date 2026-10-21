# Devbraco: what we are building and why

Devbraco is a single web front-end, deployed once per client, that captures and displays data through an
external API. Each client controls their own wording and branding through a CMS. None of them gets their
own copy of the code.

This document is the business case and the shape of the plan. A walkthrough of what exists today is in
[phase0.md](phase0.md), and the technical detail is in [IMPLEMENTATION.md](IMPLEMENTATION.md).

## The problem today

Every new client starts life as a copy of the previous client, which is then customised. That makes the
first week of a new client cheap and every week after it expensive.

Customisation currently lands in three different places:

- The database behind the external API.
- Configuration.
- **Compiled code inside each client's own repository.** The clearest example is member status: a core
  concept, where each client has their own set of statuses, and each client's codebase carries its own
  hand-written mapping from a shared enum onto that client's values.

The third one is the real problem, because compiled code in a forked repository is the one medium that
cannot be centrally updated. The consequences follow directly:

- A single bug fix or security patch has to be re-applied, re-tested and re-released once per client.
- Effort per client grows with the number of clients, so the business scales sub-linearly.
- The most recent client's quirks propagate to the next client, and the one after that, indefinitely.
- Nobody can answer "what is different about client X" without reading a diff.

## The objective

One shared codebase. Per-client configuration. No forks.

Clients get genuine, self-serve control of the things they care about day to day: wording, labels, help
text, validation messages, branding. They get no control at all over logic, structure or data contracts,
because that is what keeps twenty deployments upgradeable from one release.

The success test is simple and worth repeating to anyone who asks: **a fix is written once and released to
everyone, and a new client needs no new code.**

## The governing rule: three axes of variation

Every way in which one client differs from another belongs to exactly one of three categories, and each
category has its own owner, lifecycle and change process. Conflating them is what makes systems like this
painful.

| Axis | Who owns it | Where it lives | How it changes |
| --- | --- | --- | --- |
| Logic and structure | Us | Shared code, identical in every deployment | A release. Varies only by declared capability flags, never by client identity |
| Configuration and vocabulary | Us | A per-client profile in the shared repository | A reviewed pull request, schema-validated, checked against the live API at deploy time |
| Content, copy and theme | The client | Defaults in source control, materialised into each client's CMS | The client edits it themselves, any time, no deploy |

If a request cannot be placed on one of these three axes, that is the signal to stop and design rather than
to start typing.

## Fixing the vocabulary problem

The per-client enum mapping was the thing driving code into forks, so it gets solved explicitly.

Application logic never branches on a client's raw status values. It branches on a small, fixed set of
canonical meanings: *is active, is pending, is lapsed, can renew, requires payment prompt*. Each client's
own raw values are mapped onto those meanings in that client's configuration profile, not in compiled code.

Two things fall out of this that matter commercially:

- Adding or renaming a status for one client becomes a configuration change, reviewed in minutes, with no
  build and no release of application logic.
- When a client changes their database without telling us, an unmapped value is detected, degrades safely
  rather than failing the journey, and raises an alert. Today that class of change is discovered by a user
  hitting an error.

## How genuinely bespoke work is absorbed

"This client is different" is a legitimate request, not a failure. The architecture answers it with a
ladder, so each level of difference has a known and declining-to-rising cost, and none of the rungs is a
fork:

1. **Wording** — the client does it themselves in the CMS. No deploy, no involvement from us.
2. **Pagination and ordering** — two steps of five questions, or five steps of two, is a per-client layout
   referencing the same shared question catalogue. Configuration only.
3. **Which optional questions appear** — a capability flag per client, subject to what the API requires.
   Configuration only.
4. **A different treatment of the same question** — for instance postcode lookup instead of manual address
   entry. Shared code offering both; configuration selects.
5. **A different version of a whole journey** — a second named journey definition living in the shared
   codebase, compiled and tested alongside everything else.
6. **A truly bespoke journey for one client** — accepted divergence, still in the shared repository, in a
   clearly marked area, with its own tests.

The rule that keeps this honest: the unit of divergence is always a whole named journey, never a conditional
scattered through shared code. A client's name must never appear in shared logic, and the build fails if it
does.

## Phasing and current status

The destination described above would take a long time to build in full, and its riskiest component would
become load-bearing before anyone trusted it. So it is sequenced to deliver value early.

- **Phase 0 — thin vertical slice. Built and verified.** One journey, three question types, wording
  resolved from the CMS with code defaults as the fallback, a generic renderer, a stubbed API and a
  development sign-in that lets a developer act as any kind of member. Content is created in the CMS once
  and never overwritten. The purpose was to prove the boundary holds and that adding a journey really is a
  code-only change with no CMS configuration. It does.
- **Phase 1 — safe for many clients.** Per-client profiles and the vocabulary mapping, capability flags,
  automated checks that every client's profile is complete and matches the live API, staged release rings,
  and a per-client record of what each deployment changed.
- **Phase 2 — reconciling our updates with client edits.** Deploys currently create missing wording and
  never touch what exists. Phase 2 adds the ability to push an improved default out to clients who have not
  customised that wording, while leaving customised wording alone. It starts in report-only mode: compute
  what would change, publish the report, change nothing, and only start applying once the reports have been
  boringly correct for several releases.
- **Phase 3 — industrialise.** Automated onboarding of a new client, migration of the existing forked
  clients, and monitoring.

Phase 0 is defended aggressively against additions. The main project risk is not technical difficulty, it is
the number of moving parts that could be built before the first journey ships.

## Decisions already taken that affect cost or capability

- **No additional CMS licensing is required.** The design uses only the free, open-source parts of uSync.
  The commercial edition (around GBP 425 per project) adds site-to-site publishing, export packs and
  snapshots, none of which this design needs, because releases flow through source control and the pipeline
  instead. At twenty client sites that is roughly GBP 8,500 avoided, and site-to-site publishing would
  actively undercut the rule that every change is a reviewed artifact in source control.
- **Releases take a short, scheduled restart per client rather than being zero-downtime.** These are
  modest-traffic sites, and zero-downtime on this platform means paying for twenty additional hosting slots
  and managing a well-known set of sharp edges. Accepting the restart removes an entire class of failure,
  because only one instance is ever pointed at a client's database. The decision is reversible: what it
  would take is written down in the architecture plan so it can be revisited knowingly if a client's traffic
  or contract makes it untenable.
- **Clients edit production content directly.** We do not gate their wording changes, and we do not push our
  content changes back over theirs. Content flows from source control out to clients; a client's own content
  never flows back automatically.
- **The CMS is deliberately doing very little.** It is a versioned wording store with an editing interface
  that clients already recognise. That is the right way to use it here, but it is honest to note that we pay
  its operational weight in exchange for the editing experience, which is why the application is built so
  that the CMS can be replaced without rewriting any journey.

## Risks worth management attention

- **The content reconciler in Phase 2 is the riskiest component**, because it writes to client content on
  every deploy, so a defect could affect every client at once. It is mitigated by phasing, by a mandatory
  report-then-apply sequence with a backup beforehand, and by recording every before-and-after value so a
  bad run can be reverted. If confidence never materialises, stopping at Phase 1 behaviour — create what is
  missing, report what diverges, change nothing else — is a legitimate permanent destination.
- **Migration of the existing forked clients is the long pole**, and the first two migrations should be
  expected to change the configuration format. They are scheduled first for exactly that reason.
- **A metadata endpoint from the API team sits on the critical path** for retiring hand-maintained status
  mappings. Nothing is blocked without it, but the interim cost is per-client mappings we maintain by hand,
  so the ask should be raised early.
- **The automated guardrails are the thing preventing a slow return to forking**, not this document. They
  must be in place and enforced from the beginning; the architecture decays the first time someone writes a
  condition on a client's name.
