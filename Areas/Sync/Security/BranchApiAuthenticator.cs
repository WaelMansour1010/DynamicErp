using System;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace MyERP.Areas.Sync.Security
{
    public class BranchApiAuthenticator
    {
        private const int MaxClockSkewMinutes = 15;

        public void Validate(HttpRequestBase request, int branchId, string rawBody, string payloadHash)
        {
            if (request == null)
            {
                throw new UnauthorizedAccessException("Request is required.");
            }

            var token = ResolveToken(branchId);
            if (String.IsNullOrWhiteSpace(token))
            {
                throw new UnauthorizedAccessException("Branch token is not configured on the central server.");
            }

            var authorization = request.Headers["Authorization"];
            var timestamp = request.Headers["X-Branch-Timestamp"];
            var signature = request.Headers["X-Signature"];
            var headerBranchId = request.Headers["X-Branch-Id"];

            if (!String.IsNullOrWhiteSpace(headerBranchId) && !String.Equals(headerBranchId, Convert.ToString(branchId), StringComparison.Ordinal))
            {
                throw new UnauthorizedAccessException("Branch header does not match payload.");
            }

            ValidateTimestamp(timestamp);

            if (!String.IsNullOrWhiteSpace(signature))
            {
                var expected = Sign(token, timestamp, payloadHash, rawBody);
                if (!FixedTimeEquals(signature, expected))
                {
                    throw new UnauthorizedAccessException("Invalid branch payload signature.");
                }

                return;
            }

            if (RequireSignature())
            {
                throw new UnauthorizedAccessException("Signed branch payload is required.");
            }

            if (String.IsNullOrWhiteSpace(authorization) || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Bearer token is required.");
            }

            var supplied = authorization.Substring("Bearer ".Length).Trim();
            if (!FixedTimeEquals(supplied, token))
            {
                throw new UnauthorizedAccessException("Invalid branch token.");
            }
        }

        private static bool RequireSignature()
        {
            var value = ConfigurationManager.AppSettings["Sync.BranchApiRequireSignature"];
            bool parsed;
            return String.IsNullOrWhiteSpace(value) || !Boolean.TryParse(value, out parsed) || parsed;
        }

        private static string ResolveToken(int branchId)
        {
            var exactName = ConfigurationManager.AppSettings["Sync.BranchApiTokenEnvironmentVariable"];
            if (!String.IsNullOrWhiteSpace(exactName))
            {
                var exact = Environment.GetEnvironmentVariable(exactName, EnvironmentVariableTarget.Machine)
                    ?? Environment.GetEnvironmentVariable(exactName, EnvironmentVariableTarget.User)
                    ?? Environment.GetEnvironmentVariable(exactName);
                if (!String.IsNullOrWhiteSpace(exact))
                {
                    return exact;
                }
            }

            var prefix = ConfigurationManager.AppSettings["Sync.BranchApiTokenEnvironmentPrefix"];
            if (String.IsNullOrWhiteSpace(prefix))
            {
                prefix = "SATRIAH_BRANCH_SYNC_TOKEN_";
            }

            var name = prefix + branchId;
            return Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Machine)
                ?? Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User)
                ?? Environment.GetEnvironmentVariable(name);
        }

        private static void ValidateTimestamp(string timestamp)
        {
            DateTime parsed;
            if (String.IsNullOrWhiteSpace(timestamp) || !DateTime.TryParse(timestamp, out parsed))
            {
                throw new UnauthorizedAccessException("Valid branch timestamp is required.");
            }

            var utc = parsed.Kind == DateTimeKind.Utc ? parsed : parsed.ToUniversalTime();
            if (Math.Abs((DateTime.UtcNow - utc).TotalMinutes) > MaxClockSkewMinutes)
            {
                throw new UnauthorizedAccessException("Branch timestamp is outside the allowed replay window.");
            }
        }

        public static string Sign(string token, string timestamp, string payloadHash, string rawBody)
        {
            var material = timestamp + "." + payloadHash + "." + rawBody;
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(token ?? "")))
            {
                return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(material)));
            }
        }

        private static bool FixedTimeEquals(string left, string right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            var leftBytes = Encoding.UTF8.GetBytes(left);
            var rightBytes = Encoding.UTF8.GetBytes(right);
            var diff = leftBytes.Length ^ rightBytes.Length;
            for (var i = 0; i < leftBytes.Length && i < rightBytes.Length; i++)
            {
                diff |= leftBytes[i] ^ rightBytes[i];
            }

            return diff == 0;
        }
    }
}
