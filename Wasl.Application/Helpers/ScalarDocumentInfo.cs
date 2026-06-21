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
                The platform relies heavily on **SignalR** backed by **Redis** for high-performance, real-time location tracking and ride lifecycle management. Front-end applications MUST implement these listeners to function correctly.

                ### 🔌 1. Connecting to the Hub
                - **Endpoint URL:** `https://apiservice.ddns.net/hubs/tracking`
                - **Authentication:** Standard browser WebSockets do not support HTTP Headers. You **must** pass the JWT Access Token in the URL query string:
                  `?access_token=YOUR_JWT_TOKEN`

                ### 🚕 2. For DRIVERS: Listening Events (On)
                Drivers must listen to these events to receive and manage ride requests:
                * `ReceiveRideRequest`: Triggered when a new ride is requested nearby.
                    * **Payload (JSON):** `{ "rideId": "guid", "lat": double, "lng": double, "dropLat": double, "dropLng": double, "price": decimal }`
                * `HideRideRequest`: Triggered when a Rider cancels a *Pending* ride. The frontend MUST close the request popup if the ID matches.
                    * **Payload (String):** `rideId`
                * `RideCancelled`: Triggered when a Rider cancels a ride *after* the driver has accepted it.
                    * **Payload (String):** `message` 

                ### 👤 3. For RIDERS: Listening Events (On)
                Riders must listen to this specific event regarding cancellations:
                * `RideCancelled`: Triggered if the driver cancels the active trip.
                    * **Payload (String):** `message` 
                *(Note: Ride status updates like Accepted, Arrived, and Completed are currently managed via standard REST endpoints and polling/status checks).*

                ### 🚀 4. Client Invoking Events (Send)
                Once connected, clients can send data to the server directly via SignalR:
                * `UpdateLocation(double latitude, double longitude)`: **(Drivers Only)** Broadcasts the Driver's current GPS coordinates. Should be called periodically (e.g., every 5-10 seconds) while online or in an active ride.

                ### 🗺️ 5. Radar Simulator (Testing Tool)
                We provide a built-in web simulator to test live tracking without a mobile app.
                - **Simulator Link:** `https://apiservice.ddns.net/wasl/driver_radar_simulation.html`
                - **Usage:** Login as a Driver. The simulator will automatically connect to SignalR and start broadcasting mock GPS movements on the map, and can receive ride requests.

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