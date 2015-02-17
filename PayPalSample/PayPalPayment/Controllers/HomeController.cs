using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using PayPal.Api;
using log4net;

namespace PayPalPayment.Controllers
{
    public class HomeController : Controller
    {
        private const string PayPalSuccessLogMessage = "Visited PayPal with success got paymentId '{0}', token '{1}' payerId {2}";
        private static readonly ILog Log = LogManager.GetLogger(typeof(HomeController));

        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Prepare()
        {
            var apiContext = BuildApiContext();
            var createdPayment = Payment.Create(apiContext, BuildPayment("10", "EUR"));
            return Redirect(createdPayment.links.Single(link => link.rel == "approval_url").href);
        }

        [HttpGet]
        public ActionResult Success(string paymentId, string token, string payerId)
        {
            var message = string.Format(PayPalSuccessLogMessage, paymentId, token, payerId);
            Log.Info(message);

            Log.Info("Starting to execute payment.");
            var apiContext = BuildApiContext();
            var paymentExecution = new PaymentExecution { payer_id = payerId };
            Payment.Execute(apiContext, paymentId, paymentExecution);
            Log.Info("Executed payment.");

            var vm = new PayPalViewModel
                {
                    PayerId = payerId,
                    PaymentId = paymentId,
                    Token = token
                };
            return View(vm);
        }

        [HttpGet]
        public ActionResult Cancelled()
        {
            return View();
        }

        private Payment BuildPayment(string total, string currency)
        {
            var payer = new Payer { payment_method = "paypal" };

            var amount = new Amount
                {
                    currency = currency,
                    total = total
                };

            var transaction = new Transaction
                {
                    amount = amount,
                    description = "10 € for a T-Shirt!"
                };

            var redirectUrls = new RedirectUrls
                {
                    return_url = BuildRedirectUrl(ControllerContext.RequestContext, "Home", "Success"),
                    cancel_url = BuildRedirectUrl(ControllerContext.RequestContext, "Home", "Canceled"),
                };

            return new Payment
                {
                    intent = "sale",
                    payer = payer,
                    redirect_urls = redirectUrls,
                    transactions = new List<Transaction> { transaction }
                };
        }

        private static string BuildRedirectUrl(RequestContext requestContext, string controller, string action)
        {
            var helper = new UrlHelper(requestContext);
            var scheme = "https";
            if (requestContext.HttpContext.Request.Url != null)
            {
                scheme = requestContext.HttpContext.Request.Url.Scheme;
            }
            return helper.Action(action, controller, null, scheme);
        }

        private static APIContext BuildApiContext()
        {
            var config = ConfigManager.Instance.GetProperties();
            var accessToken = new OAuthTokenCredential(config).GetAccessToken();
            Log.Info("AccessToken is: '" + accessToken + "'.");
            return new APIContext(accessToken);
        }

        public class PayPalViewModel
        {
            public string PaymentId
            {
                get;
                set;
            }

            public string Token
            {
                get;
                set;
            }

            public string PayerId
            {
                get;
                set;
            }
        }
    }
}