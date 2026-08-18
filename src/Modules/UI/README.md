# UI module

The UI module stores the profiles a wallet intends to display in the frontend.
It replaces the browser-only profile list previously kept in local storage.

The stored relationship is an **intent**, not proof that a wallet owns a
profile. On-chain ownership is checked separately and stored in the `owned`
field as the latest known state.

## Projects

- `UI.Core` contains the profile and wallet-profile intent domain models.
- `UI.Application` contains commands, queries, contract synchronization, and
  domain-event handlers.
- `UI.Dto` contains API response models, modes, and stable error codes.
- `UI.Infrastructure` contains PostgreSQL persistence, queries, repositories,
  address normalization, and dependency registration.
- `UI.Presentation` contains the FastEndpoints API endpoints.

## Data model

```text
profiles
   1
   |
   | profile_addr
   *
wallet_profile_intents        wallet_profile_intent_events
(current frontend intent)     (append-only history)
```

### `profiles`

Caches data read from the Profile NFT contract:

- `address`: canonical Profile NFT address and primary key.
- `login`: normalized lowercase login; unique.
- `content`: profile content stored as `jsonb`.
- `updated_at`: time the cached data last changed.

The cache is refreshed when a profile is added and when a wallet's profiles
are checked. A content or login change raises an in-process
`ProfileContentChangedDomainEvent`.

### `wallet_profile_intents`

Stores the current profile-display intention of a wallet:

- `wallet_addr`: normalized TON wallet address in user-friendly,
  non-bounceable, URL-safe format.
- `profile_addr`: cached Profile NFT address.
- `mode`: `owner` or `preview`.
- `owned`: whether the wallet owned the profile during the latest successful
  contract check.
- `created_at` and `updated_at`: UTC timestamps.

The `(wallet_addr, profile_addr)` combination is unique.

Incoming wallet addresses may be raw or user-friendly, bounceable or
non-bounceable, and URL-safe or standard Base64. Every accepted representation
is converted to the user-friendly, non-bounceable, non-test-only, URL-safe form
before persistence. As a result, different textual representations of the same
TON account resolve to one wallet intent list.

Because the wallet address is a route segment, non-URL-safe Base64 input must
be URL-encoded by the client before it is sent.

`mode` and `owned` deliberately mean different things:

- `mode` records how the wallet intended to use the profile in the UI.
- `owned` records the last verified on-chain ownership state.

Losing ownership sets `owned` to `false`; it does not remove the relationship
or silently change `mode` to `preview`. This preserves the original intent.

### `wallet_profile_intent_events`

Stores append-only relationship history:

- `added`: a new display intent was created.
- `removed`: the wallet removed an existing display intent.
- `ownership_lost`: a successful check observed an `owned: true` to
  `owned: false` transition.
- `ownership_gained`: a successful check observed an `owned: false` to
  `owned: true` transition. This includes both newly acquired and restored
  ownership; it is not recorded as another `added` event.

Each event includes wallet and profile addresses, UTC occurrence time, and a
`jsonb` data object containing relevant mode and ownership values. Events are
inserted by domain-event handlers in the same `SaveChanges` operation as the
current relationship change.

An `added` event is emitted only when the display intent is first created.
Later ownership changes never emit another `added` event.

## Contract lookup

Profile resolution uses the Contracts module through MediatR request/response
queries:

1. Resolve the Profile NFT address from the normalized login.
2. Read the Profile NFT data.
3. Cache its login and content.
4. Compare its owner address with the normalized wallet address.

The UI module does not directly call Contracts infrastructure.

## API

Business failures for add, remove, and check are returned in the response body
using `success: false` and stable error codes. This lets the frontend map each
code to localized text without parsing server messages.

### Add a profile intent

```http
POST /api/ui/wallets/{wallet_addr}/profiles
Content-Type: application/json

{
  "login": "alice",
  "mode": "owner"
}
```

Valid modes are `owner` and `preview`.

Example success:

```json
{
  "success": true,
  "errors": [],
  "available_modes": ["owner", "preview"]
}
```

Behavior:

- The login is resolved through the Profile contracts before anything is
  added.
- `owner` succeeds only when the wallet currently owns the profile.
- `preview` succeeds for any wallet when the profile is valid.
- Requesting `owner` without ownership returns
  `err_contract_doesnot_belong_to_the_wallet` and offers `preview` in
  `available_modes`.
- Profile lookup failures return an empty `available_modes` array.
- Adding an existing wallet-profile relationship is idempotent: it does not
  create another `added` event.
- An existing intent's mode is not changed by another add request. To replace
  the intent mode, remove the relationship and add it again.
- Even for an existing relationship, cached content and current ownership are
  refreshed. A detected ownership loss is recorded.

### Remove a profile intent

```http
DELETE /api/ui/wallets/{wallet_addr}/profiles/{login}
```

Example success:

```json
{
  "success": true,
  "errors": [],
  "available_modes": []
}
```

Removal does not call the blockchain. This ensures a wallet can remove its UI
intent even when the contracts provider is unavailable. A successful removal
adds a `removed` history event. Removing a relationship that does not exist
returns `err_profile_relationship_not_found`.

### Check all profiles for a wallet

```http
POST /api/ui/wallets/{wallet_addr}/profiles/check
```

The profiles are checked sequentially. For each relationship, the operation:

1. Reads the current profile contract data by its cached login.
2. Refreshes cached content when it changed.
3. Refreshes the `owned` value.
4. Records `ownership_lost` for a verified `true` to `false` transition.

Example response:

```json
{
  "success": true,
  "errors": [],
  "profiles": [
    {
      "wallet_addr": "EQ...",
      "profile_addr": "EQ...",
      "login": "alice",
      "mode": "owner",
      "owned": true,
      "content": {
        "login": "alice",
        "image_url": "https://example.com/image.png"
      }
    }
  ]
}
```

Checks are partially tolerant. Successfully read profiles are updated even if
another profile fails. In that case `success` is `false`, predefined errors
are returned, and the response still contains the current stored profile list.

### List a wallet's profiles

```http
GET /api/ui/wallets/{wallet_addr}/profiles
```

Returns the current relationships joined with cached profile content. It does
not invoke the blockchain. Use the check endpoint when fresh ownership and
content information is required.

## Error codes

| Code | Meaning |
| --- | --- |
| `err_wallet_not_connected` | Wallet address was not provided. |
| `err_invalid_wallet_address` | Wallet address is not a valid TON address. |
| `err_invalid_login` | Login is empty. |
| `err_invalid_profile_mode` | Add mode is missing or invalid. |
| `err_profile_not_found` | A valid deployed profile could not be found. |
| `err_contract_request_failed` | Contract data could not be read. |
| `err_contract_doesnot_belong_to_the_wallet` | Owner mode was requested by a non-owner wallet. |
| `err_profile_relationship_not_found` | The requested current intent does not exist. |

These constants are defined in `UI.Dto/UiErrorCodes.cs` and should be mirrored
in the frontend error-code map.

## Database setup

Run:

```text
src/Modules/UI/Database/Scripts/001_create_ui_profile_intents.sql
```

Before execution, set `v_database_username` inside the script to the database
role used by the API.

The preferred configuration is a dedicated connection:

```dotenv
ConnectionStrings__UI=Host=127.0.0.1;Port=5432;Database=DATABASE_NAME;Username=DATABASE_USER;Password=DATABASE_PASSWORD
```

If `ConnectionStrings__UI` is empty or absent, the module uses
`ConnectionStrings__Programs`. Run the schema script in whichever database is
selected.

## Frontend migration

The frontend can replace its browser profile storage with this sequence:

1. On wallet connection, call the list endpoint.
2. Call check when fresh contract state is needed.
3. Use the add endpoint for owner or preview selection.
4. If owner mode fails and `available_modes` contains `preview`, show the
   localized ownership warning and allow the user to retry in preview mode.
5. Use the remove endpoint instead of deleting only local storage.
6. Treat `mode` as user intent and `owned` as the verified status when choosing
   warnings and UI capabilities.

## Authentication limitation

The endpoints currently follow the rest of the API and are anonymous. A caller
can therefore submit another wallet's address. The data represents a claimed
wallet intention until wallet-signature authentication is added. Do not use it
as cryptographic evidence of wallet behavior or ownership.
