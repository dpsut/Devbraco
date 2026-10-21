# Devbraco

Phase 0 slice: one renewal journey rendered by Razor, with copy stored in Umbraco and a local API stub.

## Run

```bash
dotnet run --project src/Devbraco.Web
```

Open http://localhost:61221/go/renewal

The local backoffice user is `dev@local.test` / `Local-dev-only-1`. You do not need the backoffice to complete the journey.

## Development member

The circumstances step appears only for a lapsed member.

- Active: `/dev/member?semantics=IsActive,CanRenew`
- Lapsed: `/dev/member?semantics=IsLapsed,RequiresPaymentPrompt`

Answers are encrypted in the server cache, expire within two hours, and are deleted on submit. Card numbers are not part of this journey.
