<%@ include file="partials/header.jspf" %>
<h1>${empty id ? 'New category' : 'Edit category'}</h1>
<f:form modelAttribute="form" method="post" cssClass="editor card"><f:errors path="*" cssClass="errors" element="div"/>
<label for="name">Name</label><f:input path="name" maxlength="100" required="required"/>
<label for="slug">Slug</label><f:input path="slug" maxlength="150" pattern="[a-z0-9]+(-[a-z0-9]+)*" required="required"/>
<div class="actions"><button type="submit">Save category</button><a href="<c:url value='/admin/categories'/>">Cancel</a></div></f:form>
<%@ include file="partials/footer.jspf" %>
