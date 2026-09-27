# TypeSafe and AI-vs-AI matches

Open the game, choose **Human vs TypeSafe** or **Laya vs TypeSafe**, and enter an
API key from [TypeSafe's console](https://console.typesafe.ai). Select
**Check key & load models** to authenticate and discover the models available to
your account. Select a model, then **Enter the arena**. The default suggestion
is `jev-latest`; the authenticated model list is authoritative.

The key stays in memory for this game session. It is not stored in PlayerPrefs,
profile JSON, the repository, telemetry or exports. Relaunching requires entering
it again unless the game process inherits `TYPESAFE_API_KEY`. Do not put a real
key in source files or share it in screenshots. This is a desktop client: it
sends the key directly to TypeSafe over HTTPS, with no intermediary game server.

**Escape → Change matchup / API key** pauses the game and lets you switch modes.
Changing matchup starts a fresh match. Ordinary rematches keep the selected
players and session credentials. Laya stays local; TypeSafe-only play can run
without installing Laya. In AI-vs-AI mode both models need to be available.

## Behavior

- The providers use the same projected state, legal actions, instructions and
  executor rules. Each controls only its own avatar and sees its own perspective.
- The existing experiment request cap applies separately to each AI player.
  Default: at most four request starts per game second per AI; action duration,
  model latency and error backoff can reduce this rate.
- TypeSafe requests use your account's allowance. The setup screen checks access
  using model discovery; it does not issue paid gameplay decisions until play starts.
- Rejected credentials, account quota or invalid requests stop automatic retries
  until reconfigured. Rate limits and transient failures use bounded backoff.
- Failures leave the player waiting. There is no substitute decision policy.
- Pausing prevents new decisions. Matchup changes abort pending requests and
  invalidate previous-round decisions; no old response can control the new match.
- AI-vs-AI shares the fixed arena camera. Escape, F2, F3 and Tab
  retain pause, experiment, export and statistics controls. Stats offer both AI
  players' action distributions and execution latency.

## API contract

Implemented against the [official OpenAPI schema](https://api.typesafe.ai/openapi.json)
and [official API reference](https://api.typesafe.ai/docs), checked 2026-09-23:

- `GET https://api.typesafe.ai/v1/models`
- `POST https://api.typesafe.ai/v1/systemone`
- `Authorization: Bearer <session key>`
- Request: `model`, structured `state`, and `questions.action` with type `choice`,
  instructions and a criteria map keyed by the allowed semantic action names.
- Response: `answers.action.choice`, `confidence`, and answer type, with the
  served model and raw probability map retained for research.

The transport requires a legal action, the correct answer type and finite
confidence in [0,1]. It never executes free-form model output.

## Validation

See [validation/MATCHUPS.md](validation/MATCHUPS.md). No live TypeSafe account key
was supplied during implementation; the cloud integration was exercised against
an isolated local HTTP contract fixture. Laya was tested with its real local model.
