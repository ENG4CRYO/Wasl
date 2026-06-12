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

                ## 📡 Real-Time Location Tracking (SignalR)
                The platform uses **SignalR** backed by **Redis** for high-performance, real-time location tracking and broadcasting.

                ### 🔌 1. Connecting to the Hub
                - **Endpoint URL:** `https://apiservice.ddns.net/wasl/hubs/tracking`
                - **Authentication:** Standard browser WebSockets do not support HTTP Headers. You **must** pass the JWT Access Token in the URL query string:
                  `?access_token=YOUR_JWT_TOKEN`
                  *(The backend automatically intercepts this and authenticates the session).*

                ### 🎧 2. Client Listening Events (Frontend -> Listen)
                Register these listeners **before** starting the connection:
                - `ReceiveLocationUpdate(double latitude, double longitude)`: Triggered when a driver updates their location.

                ### 🚀 3. Client Invoking Events (Frontend -> Send)
                Once connected, invoke these backend methods:
                - `UpdateLocation(double latitude, double longitude)`: Broadcasts the Driver's current GPS coordinates to the system.

                ### 🗺️ 4. Radar Simulator (Testing Tool)
                We provide a built-in web simulator to test live tracking without a mobile app.
                - **Simulator Link:** [https://apiservice.ddns.net/wasl/driver_radar_simulation.html](https://apiservice.ddns.net/wasl/driver_radar_simulation.html)
                - **Usage:** Login as a Driver. The simulator will automatically connect to SignalR and start broadcasting mock GPS movements on the map.

                ---

                ## 🚖 Ride Lifecycle & Business Logic
                To maintain data integrity, a strict state machine is enforced:
                1. **Pending:** Ride requested by the Rider.
                2. **Accepted:** Driver accepts the ride. *(Validation: A Driver cannot accept a new ride if they already have an Active/Accepted ride).*
                3. **InProgress:** The trip is ongoing.
                4. **Completed:** The Driver finishes the trip using the `complete` endpoint, freeing them to accept new requests.

                ---

                ## 📌 Infrastructure & Standards

                ### 🌍 Localization (Multi-language)
                The API supports **Arabic (ar)** and **English (en)**.
                - Use the `Accept-Language` header to toggle response messages.
                - Validation errors are also localized automatically.

                ### 📧 Background Jobs
                Email sending is non-blocking. We use **System.Threading.Channels** to queue emails and process them in a background worker, ensuring the API responds instantly.

                ---

                ## 📋 Request Headers
                | Header Name | Value | Description | Required? |
                | :--- | :--- | :----- | :--- |
                | **Authorization** | `Bearer {token}` | Standard JWT. | Yes (Secured) |
                | **Accept-Language** | `en` or `ar` | Default is `en`. | Optional |
                | **X-Api-Version** | `1.0` | API versioning control. | Yes |
                | **X-App-Version** | `1.0.0` | Mobile/Web app version. | Yes |

                ---

                ## ⏳ Rate Limiting
                To protect against DDoS and Brute-force:
                * **Global:** 200 requests / 10 seconds.
                * **Auth (Login/OTP):** 5 requests / 30 seconds per IP.

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

                ---

                ## 🧪 Development & Testing
                * **Environment:** `Production / Development`
                """;

            return template;
        }
    }
}