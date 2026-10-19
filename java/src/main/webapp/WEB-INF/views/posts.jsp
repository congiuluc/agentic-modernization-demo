<%@ include file="partials/header.jspf" %>
<div class="section-heading"><h1>Stories</h1><a class="button" href="<c:url value='/admin/posts/create'/>">New post</a></div>
<c:forEach items="${posts}" var="post"><article class="card admin-row" data-post-id="${post.id}">
<div><h2><c:out value="${post.title}"/></h2><p><code><c:out value="${post.slug}"/></code> / ${post.published ? 'Published' : 'Draft'}</p></div>
<div class="actions"><a href="<c:url value='/admin/posts/${post.id}/edit'/>">Edit</a>
<c:set var="operation" value="${post.published ? 'unpublish' : 'publish'}"/>
<form class="inline" method="post" action="<c:url value='/admin/posts/${post.id}/${operation}'/>"><%@ include file="partials/csrf.jspf" %><button type="submit">${post.published ? 'Unpublish' : 'Publish'}</button></form>
<a href="<c:url value='/admin/posts/${post.id}/delete'/>">Delete</a></div></article></c:forEach>
<c:if test="${empty posts}"><p class="empty">No posts yet.</p></c:if>
<nav class="pagination" aria-label="Pagination"><c:if test="${page > 1}"><a href="?page=${page - 1}">Previous</a></c:if><span>Page ${page}</span><c:if test="${page * 10 < total}"><a href="?page=${page + 1}">Next</a></c:if></nav>
<%@ include file="partials/footer.jspf" %>
