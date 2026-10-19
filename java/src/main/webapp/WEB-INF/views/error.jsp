<%@ include file="partials/header.jspf" %>
<section class="card narrow"><h1>We couldn't complete that request.</h1>
<p><c:choose><c:when test="${not empty error}"><c:out value="${error}"/></c:when><c:otherwise>The page is unavailable or the request was rejected.</c:otherwise></c:choose></p>
<a href="<c:url value='/'/>">Back to the journal</a></section>
<%@ include file="partials/footer.jspf" %>
