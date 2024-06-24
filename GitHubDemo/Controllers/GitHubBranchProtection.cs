using Microsoft.AspNetCore.Mvc;
using Octokit;
using System.Security.Cryptography;
using System.Text;

namespace GitHubDemo.Controllers
{
    [ApiController]
    [Route("webhook")]
    public class GitHubBranchProtectionController : ControllerBase
    {
        private readonly ILogger<GitHubBranchProtectionController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public GitHubBranchProtectionController(ILogger<GitHubBranchProtectionController> logger,
            IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost(Name = "EnableBranchProtection")]
        public async Task<IActionResult> Post([FromBody] dynamic payload)
        {
            try
            {
                // Retrieve the GitHub secret from configuration
                var secret = _configuration["GitHubWebhookSecret"];

                // Retrieve the signature from the headers
                var signature = Request.Headers["X-Hub-Signature-256"].FirstOrDefault();
                if (string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(secret))
                {
                    return Unauthorized();
                }

                // Compute the hash using the secret and compare it with the signature
                using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
                {
                    var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload.GetRawText()));
                    var hashString = "sha256=" + BitConverter.ToString(hash).Replace("-", "").ToLower();

                    if (!hashString.Equals(signature, StringComparison.OrdinalIgnoreCase))
                    {
                        return Unauthorized();
                    }
                }

                var eventType = Request.Headers["X-GitHub-Event"].FirstOrDefault();
                if (eventType == "repository")
                {
                    var action = payload.GetProperty("action").GetString();
                    if (action == "created")
                    {
                        var repositoryId = payload.GetProperty("repository").GetProperty("id").GetInt64();
                        var ownerName = payload.GetProperty("sender").GetProperty("login").GetString();
                        var repositoryName = payload.GetProperty("repository").GetProperty("name").GetString();
                        var defaultBranch = payload.GetProperty("repository").GetProperty("default_branch").GetString();

                        if (repositoryId <= 0 )
                        {
                            return BadRequest();
                        }
                        await SetBranchProtectionAndNotification(repositoryId, ownerName, repositoryName, defaultBranch);

                    }
                }

                return Ok(new { message = "Webhook received and processed" });
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error processing webhook");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal Server Error" });
            }
        }

        private async Task SetBranchProtectionAndNotification(long id, string owner, string repositoryName, string defaultBranch)
        {
            var token = _configuration["GitHubToken"];
            if (string.IsNullOrEmpty(defaultBranch))
            {
                defaultBranch = _configuration["DefaultBranch"] ?? "main";
            }
            

            var client = new GitHubClient(new Octokit.ProductHeaderValue("DotNet-Webhook"));
            var tokenAuth = new Credentials(token); 
            client.Credentials = tokenAuth;
           
            try
            {
                var status = await client.Repository.Branch.UpdateBranchProtection(id, defaultBranch, new BranchProtectionSettingsUpdate
                        (new BranchProtectionRequiredReviewsUpdate(true, false, 2)));

                if (status == null)
                {
                    _logger.LogError("Failed to set branch protection");
                    throw new Exception("GitHub API returned empty response");
                }
                else
                {
                    var issue = new NewIssue($"Branch Protection Update Notification");
                    issue.Body = $"@{owner} Branch Protection is Enabled for Branch: {defaultBranch}";
                    await client.Issue.Create(id, issue);
                }
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error processing webhook");
            }
        }
    }
}
