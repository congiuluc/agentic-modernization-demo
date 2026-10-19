package com.example.legacyblog.viewmodel;

import javax.validation.constraints.*;

public class CategoryForm {
    @NotBlank @Size(max = 100)
    private String name;
    @NotBlank @Size(max = 150) @Pattern(regexp = "[a-z0-9]+(-[a-z0-9]+)*")
    private String slug;
    public String getName() { return name; }
    public void setName(String value) { name = value; }
    public String getSlug() { return slug; }
    public void setSlug(String value) { slug = value; }
}
