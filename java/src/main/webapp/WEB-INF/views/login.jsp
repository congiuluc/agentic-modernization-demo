<%@ include file="partials/header.jspf" %>
<section class="narrow card"><h1>Welcome back.</h1><p>Sign in to your writing desk.</p>
<c:if test="${not empty param.error}"><p class="errors">Invalid username or password.</p></c:if>
<form id="login-form" class="editor" method="post" action="<c:url value='/account/login'/>"><%@ include file="partials/csrf.jspf" %>
<label for="username">Username</label><input id="username" name="username" maxlength="64" autocomplete="username" required>
<label for="password">Password</label><input id="password" type="password" name="password" autocomplete="current-password" required>
<button type="submit">Sign in</button></form></section>
<%@ include file="partials/footer.jspf" %>
