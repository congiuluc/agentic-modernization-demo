package com.example.legacyblog.model;

import java.time.LocalDateTime;
import javax.persistence.*;

@Entity
@Table(name = "blog_comments")
public class Comment {
    @Id @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;
    @Column(nullable = false, length = 100)
    private String author;
    @Column(nullable = false, length = 2000)
    private String body;
    @Column(nullable = false)
    private boolean approved;
    @Column(name = "created_at", nullable = false)
    private LocalDateTime createdAt;
    @ManyToOne(optional = false)
    @JoinColumn(name = "post_id")
    private Post post;

    public Long getId() { return id; }
    public String getAuthor() { return author; }
    public void setAuthor(String author) { this.author = author; }
    public String getBody() { return body; }
    public void setBody(String body) { this.body = body; }
    public boolean isApproved() { return approved; }
    public void setApproved(boolean approved) { this.approved = approved; }
    public LocalDateTime getCreatedAt() { return createdAt; }
    public void setCreatedAt(LocalDateTime value) { createdAt = value; }
    public Post getPost() { return post; }
    public void setPost(Post post) { this.post = post; }
}
