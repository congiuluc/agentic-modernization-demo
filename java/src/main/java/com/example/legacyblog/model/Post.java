package com.example.legacyblog.model;

import java.time.LocalDateTime;
import javax.persistence.*;

@Entity
@Table(name = "blog_posts")
public class Post {
    @Id @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;
    @Column(nullable = false, length = 200)
    private String title;
    @Column(nullable = false, unique = true, length = 150)
    private String slug;
    @Lob @Column(nullable = false)
    private String body;
    @Column(nullable = false)
    private boolean published;
    @Column(name = "created_at", nullable = false)
    private LocalDateTime createdAt;
    @Column(name = "updated_at", nullable = false)
    private LocalDateTime updatedAt;
    @Column(name = "published_at")
    private LocalDateTime publishedAt;
    @ManyToOne(optional = false)
    @JoinColumn(name = "category_id")
    private Category category;

    public Long getId() { return id; }
    public String getTitle() { return title; }
    public void setTitle(String title) { this.title = title; }
    public String getSlug() { return slug; }
    public void setSlug(String slug) { this.slug = slug; }
    public String getBody() { return body; }
    public void setBody(String body) { this.body = body; }
    public boolean isPublished() { return published; }
    public void setPublished(boolean published) { this.published = published; }
    public LocalDateTime getCreatedAt() { return createdAt; }
    public void setCreatedAt(LocalDateTime value) { createdAt = value; }
    public LocalDateTime getUpdatedAt() { return updatedAt; }
    public void setUpdatedAt(LocalDateTime value) { updatedAt = value; }
    public LocalDateTime getPublishedAt() { return publishedAt; }
    public void setPublishedAt(LocalDateTime value) { publishedAt = value; }
    public Category getCategory() { return category; }
    public void setCategory(Category category) { this.category = category; }
}
