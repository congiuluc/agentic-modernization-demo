<%@ include file="partials/header.jspf" %>
<section class="hero"><h1>Notes from the workbench.</h1><p>Small ideas, engineering stories, and lessons worth keeping.</p></section>
<nav class="categories" aria-label="Categories"><a href="<c:url value='/'/>">All stories</a><c:forEach items="${categories}" var="item">
<a href="<c:url value='/categories/${item.slug}'/>"><c:out value="${item.name}"/></a></c:forEach></nav>
<c:if test="${not empty category}"><h2><c:out value="${category.name}"/></h2></c:if>
<div class="post-grid"><c:forEach items="${posts}" var="post">
<article class="card post-card"><div class="meta"><c:out value="${post.category.name}"/> / <time datetime="${post.publishedAt}Z"><c:out value="${post.publishedAt}"/> UTC</time></div>
<h2><a href="<c:url value='/posts/${post.slug}'/>"><c:out value="${post.title}"/></a></h2>
<p class="excerpt"><c:out value="${post.body}"/></p><a class="read-more" href="<c:url value='/posts/${post.slug}'/>">Read story &rarr;</a></article>
</c:forEach></div>
<c:if test="${empty posts}"><p class="empty">No published stories yet.</p></c:if>
<nav class="pagination" aria-label="Pagination">
<c:if test="${page > 1}"><a href="?page=${page - 1}">Previous</a></c:if><span>Page ${page}</span>
<c:if test="${page * 10 < total}"><a href="?page=${page + 1}">Next</a></c:if></nav>
<%@ include file="partials/footer.jspf" %>
