using Microsoft.AspNetCore.Http;

namespace FileExplorerApplicationV1.Services
{
    public interface IProviderSelector
    {
        string CurrentProvider { get; }
        void SetProvider(string provider); // only method to change provider
    }

    public class ProviderSelector : IProviderSelector
    {
        private readonly IHttpContextAccessor _httpContext;
        private const string SessionKey = "CurrentProvider";

        public ProviderSelector(IHttpContextAccessor httpContext)
        {
            _httpContext = httpContext;
        }

        public string CurrentProvider
        {
            get
            {
                var session = _httpContext.HttpContext?.Session;
                return session?.GetString(SessionKey) ?? "Local";
            }
        }

        public void SetProvider(string provider)
        {
            var session = _httpContext.HttpContext?.Session;
            session?.SetString(SessionKey, provider);
        }
    }
}
