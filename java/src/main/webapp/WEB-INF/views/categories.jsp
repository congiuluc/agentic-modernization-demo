<%@ include file="partials/header.jspf" %>
<div class="section-heading"><h1>Categories</h1><a class="button" href="<c:url value='/admin/categories/create'/>">New category</a></div>
<c:if test="${not empty error}"><p class="errors"><c:out value="${error}"/></p></c:if>
<c:forEach items="${categories}" var="category"><article class="card admin-row" data-category-id="${category.id}">
<div><h2><c:out value="${category.name}"/></h2><code><c:out value="${category.slug}"/></code></div>
<div class="actions"><a href="<c:url value='/admin/categories/${category.id}/edit'/>">Edit</a>
<form class="inline" method="post" action="<c:url value='/admin/categories/${category.id}/delete'/>"><%@ include file="partials/csrf.jspf" %><button class="danger" type="submit">Delete</button></form></div></article></c:forEach>
<c:if test="${empty categories}"><p class="empty">No categories yet.</p></c:if>
<%@ include file="partials/footer.jspf" %>
