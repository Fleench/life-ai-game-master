# Game Master API Reference

The Game Master REST API is served on localhost and the LAN IP on port 7777 (configurable).
All endpoints (except registration) require API key authentication.

## Authentication
To authenticate requests, include the `X-Api-Key` header with your API key:
`X-Api-Key: <your_api_key>`

If the API key is missing or invalid, the API returns a `401 Unauthorized` using `application/problem+json`.

## Endpoints

### Apps

**POST `/v1/apps/register`**
Registers a new connected app and returns a single-use plain text API key.
- **Request Body:** `{ "appName": "String", "platform": 0 (Desktop) | 1 (Android) | 2 (IosRemote) }`
- **Response:** `{ "app": { ... }, "apiKey": "plain_text_key" }`
- **Requires Auth:** No

**POST `/v1/apps/rotate-key`**
Rotates the API key for the authenticated app. The previous key will be immediately invalidated.
- **Response:** `{ "apiKey": "new_plain_text_key" }`
- **Requires Auth:** Yes

### Player

**GET `/v1/player`**
Retrieves the player's profile.
- **Requires Permission:** `Read` on any resource.

### Points

**GET `/v1/points`**
Retrieves the current points balances (Exp and Coins).
- **Requires Permission:** `Read` on `ExpPoints` or `Coins`.

**POST `/v1/points/award`**
Awards points to the player.
- **Request Body:** `{ "resource": 0 (ExpPoints) | 1 (Coins) | 2 (PhysicalExp) | 3 (MentalExp) | 4 (EmotionalExp) | 5 (SocialExp) | 6 (SpiritualExp), "amount": Int }`
- **Requires Permission:** `Award` on the specified resource.

**POST `/v1/points/spend`**
Spends points from the player's balance.
- **Request Body:** `{ "resource": 0 (ExpPoints) | 1 (Coins) | 2 (PhysicalExp) | 3 (MentalExp) | 4 (EmotionalExp) | 5 (SocialExp) | 6 (SpiritualExp), "amount": Int }`
- **Requires Permission:** `Spend` on the specified resource.

### Inventory

**GET `/v1/inventory`**
Retrieves the player's inventory items.
- **Requires Permission:** `Read` on `Inventory`.

**POST `/v1/inventory/add`**
Adds an item to the player's inventory.
- **Request Body:** `{ "name": "String", "quantity": Int, "metadata": "JSON String" }`
- **Requires Permission:** `Award` on `Inventory`.

**POST `/v1/inventory/remove`**
Removes an item from the player's inventory.
- **Request Body:** `{ "itemId": "UUID", "quantity": Int }`
- **Requires Permission:** `Spend` on `Inventory`.

### Permissions

**GET `/v1/permissions`**
Retrieves the current permissions granted to the calling app.
- **Requires Auth:** Yes

## Errors
All errors use standard `application/problem+json` formatting. Denied actions return a `403 Forbidden` response.
