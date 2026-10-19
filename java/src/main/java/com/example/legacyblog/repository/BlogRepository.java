package com.example.legacyblog.repository;

import com.example.legacyblog.model.*;
import java.util.List;
import javax.persistence.*;
import org.springframework.stereotype.Repository;

@Repository
public class BlogRepository {
    @PersistenceContext
    private EntityManager em;

    public <T> T find(Class<T> type, Long id) { return em.find(type, id); }
    public void add(Object entity) { em.persist(entity); }
    public void remove(Object entity) { em.remove(entity); }
    public void flush() { em.flush(); }
    public boolean seeded() { return em.find(SeedState.class, "sample-v1") != null; }
    public List<Category> categories() {
        return em.createQuery("from Category order by name, id", Category.class).getResultList();
    }
    public Category categoryBySlug(String slug) {
        return em.createQuery("from Category where slug = :slug", Category.class)
                .setParameter("slug", slug).setMaxResults(1).getResultList().stream().findFirst().orElse(null);
    }
    public Post postBySlug(String slug) {
        return em.createQuery("from Post where slug = :slug", Post.class)
                .setParameter("slug", slug).setMaxResults(1).getResultList().stream().findFirst().orElse(null);
    }
    public List<Post> posts(boolean publicOnly, Long categoryId, int offset, int limit) {
        String filter = " where 1=1" + (publicOnly ? " and published = true" : "")
                + (categoryId == null ? "" : " and category.id = :category");
        TypedQuery<Post> query = em.createQuery("from Post" + filter
                + " order by publishedAt desc, id desc", Post.class);
        if (categoryId != null) { query.setParameter("category", categoryId); }
        return query.setFirstResult(offset).setMaxResults(limit).getResultList();
    }
    public long countPosts(boolean publicOnly, Long categoryId) {
        String filter = " where 1=1" + (publicOnly ? " and published = true" : "")
                + (categoryId == null ? "" : " and category.id = :category");
        TypedQuery<Long> query = em.createQuery("select count(p) from Post p" + filter, Long.class);
        if (categoryId != null) { query.setParameter("category", categoryId); }
        return query.getSingleResult();
    }
    public boolean hasCategoryPosts(Long id) {
        return countPosts(false, id) > 0;
    }
    public List<Comment> comments(Long postId, boolean approvedOnly) {
        if (postId == null) {
            return em.createQuery("from Comment where approved = false order by id desc", Comment.class).getResultList();
        }
        return em.createQuery("from Comment where post.id = :post"
                + (approvedOnly ? " and approved = true" : "") + " order by id", Comment.class)
                .setParameter("post", postId).getResultList();
    }
    public void deleteComments(Long postId) {
        em.createQuery("delete from Comment where post.id = :post").setParameter("post", postId).executeUpdate();
    }
    public Administrator admin(String username) {
        return em.createQuery("from Administrator where username = :username", Administrator.class)
                .setParameter("username", username).setMaxResults(1).getResultList().stream().findFirst().orElse(null);
    }
    public boolean hasAdmin() {
        return em.createQuery("select count(a) from Administrator a", Long.class).getSingleResult() > 0;
    }
}
