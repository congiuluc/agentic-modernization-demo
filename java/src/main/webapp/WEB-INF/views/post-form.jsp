<%@ include file="partials/header.jspf" %>
<h1>${empty id ? 'New draft' : 'Edit story'}</h1><p>Saving does not change publication status. Publish from the stories list.</p>
<f:form modelAttribute="form" method="post" cssClass="editor card">
<f:errors path="*" cssClass="errors" element="div"/>
<label for="title">Title</label><f:input path="title" maxlength="200" required="required"/>
<label for="slug">Slug</label><f:input path="slug" maxlength="150" pattern="[a-z0-9]+(-[a-z0-9]+)*" required="required"/>
<label for="categoryId">Category</label><f:select path="categoryId"><f:option value="">Select category</f:option><f:options items="${categories}" itemValue="id" itemLabel="name"/></f:select>
<label for="body">Body (plain text)</label><f:textarea path="body" rows="14" maxlength="50000" required="required"/>
<div class="actions"><button type="submit">Save post</button><a href="<c:url value='/admin/posts'/>">Cancel</a></div></f:form>
<%@ include file="partials/footer.jspf" %>
