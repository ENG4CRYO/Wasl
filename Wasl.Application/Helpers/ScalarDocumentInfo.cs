namespace Wasl.Application.Helpers
{
    public static class ScalarDocumentInfo
    {
        public static string GetScalarDocumentInfo()
        {
            string template = """
                # 🏗️ Wasl Enterprise API Guide
                Welcome to the official developer documentation for the **Wasl** platform. This API is built using **.NET 10**, following **Clean Architecture** and **CQRS** patterns with a focus on high security and performance.

                ---

                ## 🔐 Security & Authentication Flow
                We implement a **Stateless OTP Mechanism** to ensure maximum security without bloating the database with temporary records.

                ### 📧 1. Registration / Forgot Password Flow
                - **Initiation:** Call `initiate-registration` or `forgot-password`. 
                - **Verification Token:** The API returns a `RegisterToken` or `ResetToken` (GUID). This is **not** the OTP.
                - **Secure OTP:** A 6-digit code is generated using `RandomNumberGenerator` and sent via an asynchronous background job to your email.
                - **Caching:** The user data and OTP are temporarily stored in-memory (encrypted) for 10 minutes, linked to your Token.

                ### ✅ 2. Verification & Completion
                - Call `verify-registration` or `reset-password` by sending the `Token` + `OTP`.
                - Upon success, the system creates the actual database record and returns your **JWT Access Token** and **Refresh Token**.

                ---

                ## 📡 Real-Time Communication (SignalR)
                The platform relies heavily on **SignalR** with **Redis geo-location** for high-performance, real-time location tracking and ride lifecycle management. Front-end applications MUST implement these listeners to function correctly.

                ### 🔌 1. Connecting to the Hub
                - **Endpoint URL:** `https://apiservice.ddns.net/wasl/hubs/tracking`
                - **Authentication:** Standard browser WebSockets do not support HTTP Headers. You **must** pass the JWT Access Token in the URL query string:
                  `?access_token=YOUR_JWT_TOKEN`
                - **User Identity:** SignalR maps users via the `uid` claim in the JWT. This is used by `Clients.User(userId)` to send targeted events.
                - **Reconnection:** The client should use `.withAutomaticReconnect()` (built-in retry: 0s, 2s, 10s, 30s then exponential backoff). Listen for:
                  - `onreconnecting` → Show "Reconnecting..." UI state
                  - `onreconnected` → Restore connected state
                  - `onclose` → Show "Disconnected" UI state; if 401, redirect to login

                ### 📍 2. Client-Callable Hub Methods (Invoke)

                | Method | Parameters | Who | Description |
                |--------|-----------|-----|-------------|
                | `UpdateLocation` | `(double latitude, double longitude, string? rideId)` | **Driver** | Updates GPS in Redis GEO index. Call every 3–10s while online. Pass `rideId` only during an active ride to broadcast location to rider. If driver is not Approved, connection is aborted. |
                | `TrackRide` | `(string rideId)` | **Rider** | Joins the `Ride_{rideId}` group to start receiving live driver location updates. Must be called once after ride is accepted. |

                ### 🚕 3. For DRIVERS: Listening Events (On)
                Drivers must listen to these events to receive and manage ride requests:
                * `ReceiveRideRequest`: Triggered when a new ride is requested nearby.
                    * **Payload (JSON):** `{ "rideId": "guid", "lat": double, "lng": double, "dropLat": double, "dropLng": double, "calculatedPrice": decimal, "paymentMethod": "Cash" | "Card" | "Wallet", "riderName": string, "riderPhone": string, "message": string }`
                * `HideRideRequest`: Triggered when a Rider cancels a *Pending* ride. The frontend MUST close the request popup if the ID matches.
                    * **Payload (String):** `rideId`
                * `RideCancelled`: Triggered when a Rider cancels a ride *after* the driver has accepted it, or when the ride auto-cancels after 5 minutes.
                    * **Payload (String):** `message`
                * `ProfileReviewed`: Triggered instantly when an Admin approves or rejects your submitted profile.
                    * **Payload (JSON):** `{ "isApproved": boolean, "message": string }`
                    * *(Note: If `isApproved` is true, the driver account is fully activated. If false, the `message` will contain the rejection reason so the driver can fix the issues).*

                ### 👤 4. For RIDERS: Listening Events (On)
                Riders must listen to these specific events regarding their active trips to automatically update the UI without polling the server:

                * `ReceiveDriverLocation`: Triggered whenever the driver updates their live location on the map. *(Note: The rider must call `TrackRide` first to join the group).*
                    * **Payload (two positional arguments):** `latitude` (double), `longitude` (double)

                * `RideAccepted`: Triggered immediately when a driver accepts the requested trip. The app should transition from the "finding driver" state to showing driver details.
                    * **Payload (JSON):** `{ "rideId": "guid", "driverId": "string", "driverName": "string", "driverProfilePictureUrl": "string", "vehicleModel": "string", "vehicleYear": int, "vinNumber": "string", "phoneNumber": "string", "driverLatitude": double, "driverLongitude": double, "message": "string" }`

                * `DriverArrived`: Triggered when the driver taps the "Arrived" button. The app should display a notification for the rider to head out.
                    * **Payload (JSON):** `{ "rideId": "guid", "message": "string" }`

                * `RideStarted`: Triggered when the rider boards the car and the trip officially begins. The app should transition to the "In Transit" state.
                    * **Payload (JSON):** `{ "rideId": "guid", "message": "string" }`

                * `RideCompleted`: Triggered when the driver successfully ends the trip. The app should close the map and display the receipt and rating screen.
                    * **Payload (JSON):** `{ "rideId": "guid", "message": "string" }`

                * `RideCancelled`: Triggered if the driver or the system cancels the active trip. The app should return to the home screen and display the reason.
                    * **Payload (String):** `message`


                ### 🚀 5. Connection Lifecycle & Driver Approval
                - **On connect:** For Drivers, the server checks `ApprovalStatus`. If not **Approved**, the connection is aborted immediately.
                - **On disconnect:** The driver's location is automatically removed from the Redis GEO index, making them invisible for new ride requests.
                - **Rider connections:** No special validation on connect; riders need only a valid JWT.

                ### 🗺️ 6. Ride Dispatch Mechanism
                When a ride is requested, the system finds nearby drivers via a background job:

                - **Radius expansion:** Starts at **2km**, increases by **+2km every 60 seconds** (up to 10km max).
                - **Excluded drivers:** Already-notified drivers are tracked in Redis (`ride:{rideId}:excluded`, TTL: 10 min) to avoid duplicate notifications.
                - **Auto-cancel:** If no driver accepts within **5 minutes**, the ride status changes to `Cancelled` automatically.
                - **Rider cancellation (Pending):** Sends `HideRideRequest` to all notified drivers so they can remove the popup.
                - **Race condition protection:** Ride acceptance uses a **distributed Redis lock** (`RideLock:{rideId}`) with 5-minute TTL to prevent two drivers accepting the same ride.

                ### 🗺️ 7. WebSocket CORS Requirement
                The API CORS policy includes `.AllowCredentials()` which is **required** for SignalR WebSocket connections. Ensure your frontend's requests include credentials (e.g. `withCredentials` in fetch or `accessTokenFactory` in SignalR).

                ### 🔌 8. Summary of All Events & Payloads

                | # | Event Name | Sent To | Payload |
                |---|-----------|---------|---------|
                | 1 | `ReceiveRideRequest` | Drivers (nearby) | `{ "rideId", "lat", "lng", "dropLat", "dropLng", "calculatedPrice", "paymentMethod", "riderName", "riderPhone", "message" }` |
                | 2 | `HideRideRequest` | Drivers (notified) | `string (rideId)` |
                | 3 | `RideCancelled` | Rider or Driver | `string (message)` |
                | 4 | `ProfileReviewed` | Driver | `{ "isApproved": bool, "message": string }` |
                | 5 | `ReceiveDriverLocation` | Ride Group | Positional args: `latitude, longitude` |
                | 6 | `RideAccepted` | Rider | `{ "rideId", "driverId", "driverName", "driverProfilePictureUrl", "vehicleModel", "vehicleYear", "vinNumber", "phoneNumber", "driverLatitude", "driverLongitude", "message" }` |
                | 7 | `DriverArrived` | Rider | `{ "rideId", "message" }` |
                | 8 | `RideStarted` | Rider | `{ "rideId", "message" }` |
                | 9 | `RideCompleted` | Rider | `{ "rideId", "message" }` |

                ---

                ## 🚖 Ride Lifecycle & Business Logic
                To maintain data integrity, a strict state machine is enforced:
                1. **Pending:** Ride requested by the Rider.
                2. **Accepted:** Driver accepts the ride via `POST /api/v1/Rides/{id}/accept`.
                3. **DriverArrived:** Driver reaches the pickup location via `POST /api/v1/Rides/{id}/arrive`.
                4. **Started:** The passenger is in the car and the trip begins via `POST /api/v1/Rides/{id}/start`.
                5. **Completed:** The Driver finishes the trip via `POST /api/v1/Rides/{id}/complete`.
                6. **Cancelled:** Can be triggered by Rider or Driver using their specific endpoints.

                ---

                ## 📌 Infrastructure & Standards

                ### 🌍 Localization (Multi-language)
                The API supports **Arabic (ar)** and **English (en)**.
                - Use the `Accept-Language` header to toggle response messages.
                - Validation errors are also localized automatically.

                ### 📧 Background Jobs
                Email sending is non-blocking. We use background workers to queue emails and process them asynchronously, ensuring the API responds instantly.

                ---

                ## 💰 Wallet & Payment System

                The platform supports multiple payment methods for ride settlement.

                ### PaymentMethod Enum

                | Value | Description |
                |-------|-------------|
                | `Cash` (1) | Rider pays cash to the driver upon drop-off. |
                | `Card` (2) | Payment is processed via an external card gateway. |
                | `Wallet` (3) | Payment is deducted from the rider's wallet balance. |

                ### Financial Settlement on Ride Completion

                When a driver completes a ride, the system performs financial routing based on `PaymentMethod`:

                - **Cash:** Rider pays cash directly. The company commission is deducted from the driver's wallet. Drivers may carry a **negative balance** (debt) for cash rides.
                - **Card:** The card is processed externally. The driver's net earnings (total fare − commission) are credited to their wallet.
                - **Wallet:** Total fare is transferred from rider → driver wallet. Commission is then deducted from the driver.

                ### WalletTransaction Ledger

                Every balance change creates an immutable `WalletTransaction` record with:

                | Field | Description |
                |-------|-------------|
                | `Id` | Unique GUID |
                | `UserId` | The affected user |
                | `Amount` | Positive (credit) or negative (debit) |
                | `Type` | `RidePayment`, `CompanyCommission`, `WalletTopUp`, `CashOut`, `Refund` |
                | `RideId` | Optional link to the ride |

                **Note:** Riders cannot have negative balances. If a wallet payment is requested with insufficient funds, the ride request is rejected.

                ### Invisible Payments Flow (Card)

                For card payments, the platform uses the industry-standard tokenization pattern (similar to Stripe/Uber):

                1. **Tokenize:** Rider calls `POST /api/v1/Payments/tokenize` with card details → receives a one-time-use GUID token.
                2. **Request ride:** Rider calls `POST /api/v1/Rides/request` with `paymentMethod: 2` and `paymentToken: "guid"` → token stored on the ride.
                3. **Complete ride:** Driver calls `POST /api/v1/Rides/{id}/complete` (no body needed) → system reads the stored token and processes payment.

                The driver **never** sees the rider's card details. The payment token is consumed on first use.

                ### Test Cards (MockGateway)

                The development environment uses a mock payment gateway. Use these test card numbers:

                | Card Prefix | Tokenization | Payment Result | Use Case |
                |-------------|-------------|----------------|----------|
                | `4242` | ✅ Accepted | ✅ **Success** | Happy path — payment goes through |
                | `5555` | ✅ Accepted | ❌ **Declined — Insufficient funds** | Test failed payment → driver changes to Cash |
                | `1111` | ✅ Accepted | ❌ **Declined — Expired card** | Test expired card scenario |
                | Any other | ❌ Rejected | N/A — error returned | Test invalid card validation |

                **Important:** Tokens are single-use via `TryRemove`. A second attempt with the same token returns "Invalid or expired payment token."

                ---

                ## 📋 Request Headers
                | Header Name | Value | Description | Required? |
                | :--- | :--- | :----- | :--- |
                | **Authorization** | `Bearer {token}` | Standard JWT. | Yes (Secured) |
                | **Accept-Language** | `en` or `ar` | Default is `ar`. | Optional |

                ---

                ## ⚙️ Standard Response Wrapper (`ApiResponse<T>`)
                All endpoints return a unified JSON structure to simplify Frontend integration.

                ### 🟢 Success Response (200 OK)
                ```json
                {
                  "succeeded": true,
                  "message": "Operation successful.",
                  "errors": {},
                  "data": { ... } 
                }
                ```

                ### 🔴 Validation Failure (400 BadRequest)
                The `errors` field is a `Dictionary<string, List<string>>` where the key is the field name.
                ```json
                {
                  "succeeded": false,
                  "message": "Validation Errors Occurred.",
                  "errors": {
                    "Email": ["Invalid email format."],
                    "Password": ["Must be at least 6 characters."]
                  },
                  "data": null 
                }
                ```
                """;

            return template;
        }
    }
}