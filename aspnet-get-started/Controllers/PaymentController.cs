using System;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using aspnet_get_started.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace aspnet_get_started.Controllers
{
    public class PaymentController : Controller
    {
        private const string XenditInvoicesEndpoint = "https://api.xendit.co/v2/invoices";

        [HttpGet]
        public ActionResult Index()
        {
            var model = new XenditPaymentViewModel
            {
                AccountNumber = "FC-2048-9941",
                CustomerEmail = "subscriber@example.com",
                Amount = 799000,
                Description = "FiberConnect Home Fiber monthly bill"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateInvoice(XenditPaymentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            var apiKey = ConfigurationManager.AppSettings["XenditApiKey"];
            if (String.IsNullOrWhiteSpace(apiKey) || apiKey == "REPLACE_WITH_XENDIT_SECRET_KEY")
            {
                ModelState.AddModelError("", "XenditApiKey is not configured. Add your Xendit secret key in Web.config before creating live invoices.");
                return View("Index", model);
            }

            var payload = new
            {
                external_id = "fiberconnect-" + model.AccountNumber + "-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
                amount = model.Amount,
                payer_email = model.CustomerEmail,
                description = model.Description,
                success_redirect_url = Url.Action("Success", "Payment", null, Request.Url.Scheme),
                failure_redirect_url = Url.Action("Failed", "Payment", null, Request.Url.Scheme),
                should_send_email = true
            };

            using (var client = new HttpClient())
            {
                var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(apiKey + ":"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(XenditInvoicesEndpoint, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError("", "Xendit invoice creation failed: " + responseBody);
                    return View("Index", model);
                }

                var invoice = JObject.Parse(responseBody);
                var invoiceUrl = invoice.Value<string>("invoice_url");

                if (String.IsNullOrWhiteSpace(invoiceUrl))
                {
                    ModelState.AddModelError("", "Xendit returned an invoice without an invoice_url.");
                    return View("Index", model);
                }

                return Redirect(invoiceUrl);
            }
        }

        [HttpPost]
        public async Task<ActionResult> Callback()
        {
            var configuredToken = ConfigurationManager.AppSettings["XenditCallbackToken"];
            var receivedToken = Request.Headers["x-callback-token"];

            if (!String.IsNullOrWhiteSpace(configuredToken) && configuredToken != "REPLACE_WITH_XENDIT_CALLBACK_TOKEN" && configuredToken != receivedToken)
            {
                return new HttpStatusCodeResult(401, "Invalid Xendit callback token");
            }

            string body;
            using (var reader = new StreamReader(Request.InputStream))
            {
                body = await reader.ReadToEndAsync();
            }

            System.Diagnostics.Trace.TraceInformation("Xendit callback received: " + body);

            return new HttpStatusCodeResult(200);
        }

        [HttpGet]
        public ActionResult Success()
        {
            ViewBag.PaymentStatus = "Payment successful";
            ViewBag.PaymentMessage = "Thanks. Your FiberConnect payment was completed through Xendit.";
            return View("Status");
        }

        [HttpGet]
        public ActionResult Failed()
        {
            ViewBag.PaymentStatus = "Payment not completed";
            ViewBag.PaymentMessage = "Your Xendit checkout was cancelled or failed. Please try again or contact support.";
            return View("Status");
        }
    }
}
