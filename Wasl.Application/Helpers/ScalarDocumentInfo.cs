namespace Wasl.Application.Helpers
{
    public static class ScalarDocumentInfo
    {
        public static string GetScalarDocumentInfo()
        {
            string template = """
                # 🏗️ Wasl Enterprise API Guide
                Welcome to the official developer documentation for the **Wasl** platform. This API is built using **.NET 9**, following **Clean Architecture** and **CQRS** patterns with a focus on high security and performance.

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

                ## 📌 Infrastructure & Standards

                ### 🌍 Localization (Multi-language)
                The API supports **Arabic (ar)** and **English (en)**.
                - Use the `Accept-Language` header to toggle response messages.
                - Validation errors are also localized automatically.

                ### 📧 Background Jobs
                Email sending is non-blocking. We use **System.Threading.Channels** to queue emails and process them in a background worker, ensuring the API responds instantly to the user.

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

                ### 💡 Frontend Implementation Tips:
                1. **Unified Error Handling:** Create a global interceptor to check `succeeded == false`.
                2. **Field Mapping:** If `errors` has keys, map them to your form input error labels.
                3. **Toasts:** If `errors` is empty but `succeeded` is false, show the `message` in a Toast/Snackbar.

                ---

                ## 🧪 Development & Testing
                * **Environment:** `Development`
                * **Test Credentials:** * Email: `tester@wasl.com`
                  * Static OTP: `123456` (Note: Only enabled in Mock/Dev mode).
                """;

            return template;
        }
    }
}