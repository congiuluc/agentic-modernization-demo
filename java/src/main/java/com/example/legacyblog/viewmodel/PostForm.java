package com.example.legacyblog.viewmodel;

import javax.validation.constraints.*;

public class PostForm {
    @NotBlank @Size(max = 200)
    private String title;
    @NotBlank @Size(max = 150) @Pattern(regexp = "[a-z0-9]+(-[a-z0-9]+)*")
    private String slug;
    @NotBlank @Size(max = 50000)
    private String body;
    @NotNull @Positive
    private Long categoryId;

    public String getTitle() { return title; }
    public void setTitle(String value) { title = value; }
    public String getSlug() { return slug; }
    public void setSlug(String value) { slug = value; }
    public String getBody() { return body; }
    public void setBody(String value) { body = value; }
    public Long getCategoryId() { return categoryId; }
    public void setCategoryId(Long value) { categoryId = value; }
}
