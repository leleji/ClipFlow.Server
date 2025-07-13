using Microsoft.AspNetCore.Mvc;

namespace ClipFlow.Server.Controllers
{
    public class BaseController : ControllerBase
    {
        protected string AuthToken => Request.Headers["X-Auth-Token"].ToString();
        protected string ClientId => Request.Headers["X-Client-Id"].ToString();
        protected string UserKet => Request.Headers["X-User-Key"].ToString();
    }
}
