// Deliberately vulnerable, source-only training fixture. Never include in the blog build.
using System;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace LegacyBlog.TrainingOnly
{
    public static class VulnerableExamples
    {
        // CWE-89: replace concatenation with a parameterized query.
        public static string ReadTitle(SqlConnection connection, string untrustedSlug)
        {
            using (var command = new SqlCommand("SELECT Title FROM Posts WHERE Slug = '" + untrustedSlug + "'", connection))
            {
                return Convert.ToString(command.ExecuteScalar());
            }
        }

        // CWE-79: encode untrusted text before writing HTML.
        public static void WriteHeading(HttpResponse response, string untrustedTitle)
        {
            response.Write("<h1>" + untrustedTitle + "</h1>");
        }

        // CWE-759/CWE-328: use a salted adaptive password-hashing scheme, not MD5.
        public static string HashSyntheticPassword(string syntheticPassword)
        {
            using (var hash = MD5.Create())
            {
                return Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(syntheticPassword)));
            }
        }
    }
}
