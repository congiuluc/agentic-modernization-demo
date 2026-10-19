package com.example.legacyblog.service;

import com.example.legacyblog.model.*;
import com.example.legacyblog.repository.BlogRepository;
import com.example.legacyblog.viewmodel.*;
import java.time.*;
import java.nio.charset.StandardCharsets;
import java.util.List;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
@Transactional
public class BlogService {
    private final BlogRepository repository;
    private final PasswordEncoder passwords;
    public BlogService(BlogRepository repository, PasswordEncoder passwords) {
        this.repository = repository; this.passwords = passwords;
    }
    public List<Category> categories() { return repository.categories(); }
    public Category category(Long id) {
        Category value = repository.find(Category.class, id);
        if (value == null) { throw new BlogProblem(404, "Category not found."); }
        return value;
    }
    public Category categoryBySlug(String slug) {
        Category value = repository.categoryBySlug(slug);
        if (value == null) { throw new BlogProblem(404, "Category not found."); }
        return value;
    }
    public Post post(Long id) {
        Post value = repository.find(Post.class, id);
        if (value == null) { throw new BlogProblem(404, "Post not found."); }
        return value;
    }
    public Post publicPost(String slug) {
        Post value = repository.postBySlug(slug);
        if (value == null || !value.isPublished()) { throw new BlogProblem(404, "Post not found."); }
        return value;
    }
    public int page(String input) {
        try {
            if (!input.matches("[0-9]+")) { throw new NumberFormatException(); }
            int value = Integer.parseInt(input);
            if (value < 1 || value > Integer.MAX_VALUE / 10) { throw new NumberFormatException(); }
            return value;
        } catch (NumberFormatException ex) { throw new BlogProblem(400, "Page must be a positive integer."); }
    }
    public long count(boolean publicOnly, Long category) { return repository.countPosts(publicOnly, category); }
    public List<Post> posts(boolean publicOnly, Long category, int page) {
        long count = count(publicOnly, category);
        if (page > 1 && ((long) page - 1) * 10 >= count) { throw new BlogProblem(404, "Page not found."); }
        return repository.posts(publicOnly, category, (page - 1) * 10, 10);
    }
    public boolean postSlugTaken(String slug, Long exceptId) {
        Post value = repository.postBySlug(slug);
        return value != null && !value.getId().equals(exceptId);
    }
    public boolean categorySlugTaken(String slug, Long exceptId) {
        Category value = repository.categoryBySlug(slug);
        return value != null && !value.getId().equals(exceptId);
    }
    public void savePost(Long id, PostForm form) {
        if (postSlugTaken(form.getSlug(), id)) { throw new BlogProblem(409, "Post slug already exists."); }
        Category category = category(form.getCategoryId());
        Post value = id == null ? new Post() : post(id);
        value.setTitle(form.getTitle().trim()); value.setSlug(form.getSlug());
        value.setBody(form.getBody().trim()); value.setCategory(category);
        LocalDateTime now = LocalDateTime.now(ZoneOffset.UTC);
        if (id == null) { value.setCreatedAt(now); }
        value.setUpdatedAt(now);
        if (id == null) { repository.add(value); }
        repository.flush();
    }
    public void publish(Long id, boolean publish) {
        Post value = post(id);
        if (value.isPublished() != publish) {
            value.setPublished(publish);
            value.setPublishedAt(publish ? LocalDateTime.now(ZoneOffset.UTC) : null);
            value.setUpdatedAt(LocalDateTime.now(ZoneOffset.UTC));
        }
    }
    public void deletePost(Long id) {
        Post value = post(id);
        repository.deleteComments(id); repository.remove(value);
    }
    public void saveCategory(Long id, CategoryForm form) {
        if (categorySlugTaken(form.getSlug(), id)) { throw new BlogProblem(409, "Category slug already exists."); }
        Category value = id == null ? new Category() : category(id);
        value.setName(form.getName().trim()); value.setSlug(form.getSlug());
        if (id == null) { repository.add(value); }
        repository.flush();
    }
    public void deleteCategory(Long id) {
        Category value = category(id);
        if (repository.hasCategoryPosts(id)) { throw new BlogProblem(409, "This category is assigned to a post and cannot be deleted."); }
        repository.remove(value);
    }
    public List<Comment> approvedComments(Long id) { return repository.comments(id, true); }
    public List<Comment> pendingComments() { return repository.comments(null, false); }
    public void comment(String slug, CommentForm form) {
        Post post = publicPost(slug);
        Comment value = new Comment(); value.setPost(post);
        value.setAuthor(form.getAuthor().trim()); value.setBody(form.getBody().trim());
        value.setCreatedAt(LocalDateTime.now(ZoneOffset.UTC)); repository.add(value);
    }
    public void moderate(Long id, boolean delete) {
        Comment value = repository.find(Comment.class, id);
        if (value == null) { throw new BlogProblem(404, "Comment not found."); }
        if (delete) { repository.remove(value); } else { value.setApproved(true); }
    }
    public Administrator administrator(String username) { return repository.admin(username); }
    public void bootstrap(String username, String password) {
        if (!repository.hasAdmin()) {
            if (!username.matches("[A-Za-z0-9._-]{3,64}") || password.length() < 12 || password.getBytes(StandardCharsets.UTF_8).length > 72) {
                throw new IllegalStateException("Set BLOG_ADMIN_USER (3-64 letters/digits/._-) and BLOG_ADMIN_PASSWORD (12-72 characters) before first startup.");
            }
            Administrator admin = new Administrator(); admin.setUsername(username);
            admin.setPasswordHash(passwords.encode(password)); repository.add(admin);
        }
        if (repository.seeded()) { return; }
        Category notes = new Category(); notes.setName("Engineering"); notes.setSlug("engineering"); repository.add(notes);
        Category journal = new Category(); journal.setName("Journal"); journal.setSlug("journal"); repository.add(journal);
        Post first = null;
        for (int i = 1; i <= 14; i++) {
            Post value = new Post(); value.setTitle(i <= 12 ? "Legacy notes " + i : "Draft notes " + (i - 12));
            value.setSlug(i <= 12 ? "legacy-notes-" + i : "draft-notes-" + (i - 12));
            value.setBody("A small engineering journal built with a traditional layered monolith.\n\nEntry " + i + " is ready for your next modernization exercise.");
            value.setCategory(i % 2 == 0 ? journal : notes);
            LocalDateTime created = LocalDateTime.of(2020, 1, 1, 12, 0).plusDays(i);
            value.setCreatedAt(created); value.setUpdatedAt(created); value.setPublished(i <= 12);
            value.setPublishedAt(i <= 12 ? created : null); repository.add(value);
            if (first == null) { first = value; }
        }
        for (boolean approved : new boolean[] {true, false}) {
            Comment value = new Comment(); value.setPost(first); value.setAuthor("Sample reader");
            value.setBody(approved ? "Welcome to the legacy blog." : "A sample comment awaiting approval.");
            value.setApproved(approved); value.setCreatedAt(LocalDateTime.of(2020, 2, 1, 12, 0)); repository.add(value);
        }
        repository.add(new SeedState("sample-v1"));
    }
}
