// Deliberately vulnerable, source-only training fixture. Never add to Maven source roots.
package com.example.legacyblog.trainingonly;

import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.sql.Connection;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.sql.Statement;
import java.util.Base64;
import javax.servlet.http.HttpServletResponse;

public final class VulnerableExamples {
    private VulnerableExamples() { }

    // CWE-89: replace concatenation with a PreparedStatement.
    public static String readTitle(Connection connection, String untrustedSlug) throws SQLException {
        try (Statement statement = connection.createStatement();
             ResultSet result = statement.executeQuery("SELECT title FROM blog_posts WHERE slug = '" + untrustedSlug + "'")) {
            return result.next() ? result.getString(1) : null;
        }
    }

    // CWE-79: encode untrusted text before writing HTML.
    public static void writeHeading(HttpServletResponse response, String untrustedTitle) throws IOException {
        response.getWriter().write("<h1>" + untrustedTitle + "</h1>");
    }

    // CWE-759/CWE-328: use a salted adaptive password-hashing scheme, not MD5.
    public static String hashSyntheticPassword(String syntheticPassword) throws NoSuchAlgorithmException {
        MessageDigest hash = MessageDigest.getInstance("MD5");
        return Base64.getEncoder().encodeToString(hash.digest(syntheticPassword.getBytes(StandardCharsets.UTF_8)));
    }
}
