using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

namespace olx_be_api.Helpers
{
    public static class FirebaseAppHelper
    {
        private static bool _initialized = false;
        public static void Initialize()
        {
            if (_initialized || FirebaseApp.DefaultInstance != null)
            {
                return;
            }

            try
            {
                var envJson = Environment.GetEnvironmentVariable("FIREBASE_CONFIG_JSON");

                AppOptions options;
                if (!string.IsNullOrEmpty(envJson))
                {
                    options = new AppOptions()
                    {
                        Credential = GoogleCredential.FromJson(envJson)
                    };
                }
                else
                {
                    var localPath = Path.Combine(Directory.GetCurrentDirectory(), "firebase-adminsdk.json");
                    options = new AppOptions()
                    {
                        Credential = GoogleCredential.FromFile(localPath)
                    };
                }

                FirebaseApp.Create(options);
                _initialized = true;
            } catch (Exception e)
            {
                throw new Exception("Failed to initialize FirebaseApp", e);
            }

        }
    }
}
