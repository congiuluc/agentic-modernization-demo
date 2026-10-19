package com.example.legacyblog.viewmodel;

import javax.validation.constraints.*;

public class CommentForm {
    @NotBlank @Size(max = 100)
    private String author;
    @NotBlank @Size(max = 2000)
    private String body;
    public String getAuthor() { return author; }
    public void setAuthor(String value) { author = value; }
    public String getBody() { return body; }
    public void setBody(String value) { body = value; }
}
