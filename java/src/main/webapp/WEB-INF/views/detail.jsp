<%@ include file="partials/header.jspf" %>
<article class="story"><div class="meta"><a href="<c:url value='/categories/${post.category.slug}'/>"><c:out value="${post.category.name}"/></a> / <time datetime="${post.publishedAt}Z"><c:out value="${post.publishedAt}"/> UTC</time></div>
<h1><c:out value="${post.title}"/></h1><div class="prose"><c:out value="${post.body}"/></div></article>
<section class="comments"><h2>Conversation</h2>
<c:forEach items="${comments}" var="comment"><article class="card comment"><strong><c:out value="${comment.author}"/></strong><p class="prose"><c:out value="${comment.body}"/></p></article></c:forEach>
<c:if test="${empty comments}"><p>No approved comments yet.</p></c:if>
<c:if test="${param.submitted == '1'}"><p class="notice">Your comment is awaiting approval.</p></c:if>
<h3>Leave a comment</h3><p>Comments appear after administrator approval.</p>
<f:form modelAttribute="form" method="post" action="${pageContext.request.contextPath}/posts/${post.slug}/comments" cssClass="editor">
<f:errors path="*" cssClass="errors" element="div"/>
<label for="author">Name</label><f:input path="author" maxlength="100" required="required"/>
<label for="body">Comment</label><f:textarea path="body" maxlength="2000" rows="4" required="required"/>
<button type="submit">Submit comment</button></f:form></section>
<%@ include file="partials/footer.jspf" %>
