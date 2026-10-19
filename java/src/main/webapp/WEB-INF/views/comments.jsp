<%@ include file="partials/header.jspf" %>
<h1>Pending comments</h1><p>Only approved comments are visible to readers.</p>
<c:forEach items="${comments}" var="comment"><article class="card admin-row" data-comment-id="${comment.id}">
<div><h2><c:out value="${comment.author}"/></h2><p>On <c:out value="${comment.post.title}"/></p><p class="prose"><c:out value="${comment.body}"/></p></div>
<div class="actions"><form class="inline" method="post" action="<c:url value='/admin/comments/${comment.id}/approve'/>"><%@ include file="partials/csrf.jspf" %><button type="submit">Approve</button></form>
<form class="inline" method="post" action="<c:url value='/admin/comments/${comment.id}/delete'/>"><%@ include file="partials/csrf.jspf" %><button class="danger" type="submit">Delete</button></form></div></article></c:forEach>
<c:if test="${empty comments}"><p class="empty">All caught up. No pending comments.</p></c:if>
<%@ include file="partials/footer.jspf" %>
