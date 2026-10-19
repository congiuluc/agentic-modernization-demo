<%@ include file="partials/header.jspf" %>
<section class="card narrow"><h1>Delete story?</h1><p><c:out value="${post.title}"/></p><p>This permanently deletes the post and its comments.</p>
<form method="post"><%@ include file="partials/csrf.jspf" %><button class="danger" type="submit">Delete post</button> <a href="<c:url value='/admin/posts'/>">Cancel</a></form></section>
<%@ include file="partials/footer.jspf" %>
